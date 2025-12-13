using Application.Services;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class UploadService : IUploadService
    {
        private readonly string _root;
        private readonly string _contentRoot;
        private readonly ConcurrentDictionary<string, UploadInfo> _uploads;
        private readonly ConcurrentDictionary<string, string> _completed = new();
        private readonly ConcurrentDictionary<string, object> _locks = new(); // Separate dictionary for locks
        private readonly ILogger<UploadService> _logger;
        private readonly IOptionsMonitor<FileSettings> _configs;
        private const long DefaultChunkSize = 50L * 1024 * 1024; //50MB
        private const int BufferSize = 64 * 1024; // 64KB buffer for better performance

        public UploadService(IConfiguration config, IWebHostEnvironment env, ConcurrentDictionary<string, UploadInfo> uploads, ILogger<UploadService> logger, IOptionsMonitor<FileSettings> configs)
        {
            _root = config["UploadRoot"] ?? Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(_root);
            _contentRoot = env.ContentRootPath;
            _uploads = uploads;
            _logger = logger;
            _configs = configs;
        }

        public async Task<(string uploadId, long chunkSize)> InitiateAsync(string schemeName, string? fileName, long? totalBytes)
        {
            return await Task.FromResult(Initiate(schemeName, fileName, totalBytes));
        }

        public (string uploadId, long chunkSize) Initiate(string schemeName, string? fileName, long? totalBytes)
        {
            schemeName = schemeName.ToLowerInvariant();
            // ensure scheme configuration exists but intentionally skip extension validation
            var cfg = _configs.Get(schemeName);
            if (string.IsNullOrWhiteSpace(cfg.StoragePath) || string.IsNullOrWhiteSpace(cfg.RequestPath))
                throw new InvalidOperationException($"Upload scheme '{schemeName}' is not configured.");

            var id = Guid.NewGuid().ToString("N");
            var folder = Path.Combine(_root, id);
            Directory.CreateDirectory(folder);
            var info = new UploadInfo
            {
                Folder = folder,
                ChunkSize = DefaultChunkSize,
                FileName = fileName,
                TotalBytes = totalBytes,
                SchemeName = schemeName,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };
            _uploads[id] = info;
            return (id, info.ChunkSize);
        }

        public async Task<UploadStatus> UploadChunkAsync(string uploadId, int index, Stream data, long? chunkLength = null, CancellationToken cancellationToken = default)
        {
            return await SaveChunkAsync(uploadId, index, data, chunkLength, cancellationToken);
        }

        public async Task<UploadStatus> SaveChunkAsync(string uploadId, int index, Stream data, long? chunkLength = null, CancellationToken cancellationToken = default)
        {
            if (index < 0) throw new ArgumentException("Invalid chunk index", nameof(index));
            if (!_uploads.TryGetValue(uploadId, out var info))
                throw new KeyNotFoundException($"Upload {uploadId} not found");

            var max = info.ChunkSize + 2 * 1024 * 1024;
            if (chunkLength.HasValue && chunkLength.Value > max)
                throw new InvalidOperationException("Chunk exceeds maximum size.");

            // Update last activity
            info.LastActivity = DateTime.UtcNow;

            var partPath = Path.Combine(info.Folder, $"{index}.part");
            var tempPath = partPath + ".tmp";

            // Use a lock to prevent concurrent access to the same chunk
            var lockKey = $"{uploadId}_{index}";
            var lockObj = _locks.GetOrAdd(lockKey, _ => new object());

            lock (lockObj)
            {
                // Check if chunk already exists
                if (File.Exists(partPath))
                {
                    _logger.LogWarning("Chunk {Index} already exists for upload {UploadId}", index, uploadId);
                    return GetStatus(uploadId);
                }
            }

            try
            {
                // Create the file with proper disposal
                FileStream? fs = null;
                try
                {
                    fs = File.Create(tempPath, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

                    // Use ArrayPool for better memory management
                    var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                    try
                    {
                        int read;
                        while ((read = await data.ReadAsync(buffer, 0, BufferSize, cancellationToken)) > 0)
                        {
                            await fs.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                finally
                {
                    // Ensure the file stream is properly disposed
                    fs?.Dispose();
                }

                // Wait a moment to ensure the file is fully written and closed
                await Task.Delay(10, cancellationToken);

                // Atomic move to final location with retry logic
                var retryCount = 0;
                const int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Move(tempPath, partPath, overwrite: true);
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException("Temporary file was not created");
                        }
                    }
                    catch (IOException) when (retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        await Task.Delay(50 * retryCount, cancellationToken); // Exponential backoff
                        continue;
                    }
                }

                return GetStatus(uploadId);
            }
            catch (Exception ex)
            {
                // Cleanup temp file on error
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                _logger.LogError(ex, "Failed to save chunk {Index} for upload {UploadId}", index, uploadId);
                throw;
            }
        }

        public async Task<UploadStatus> GetStatusAsync(string uploadId)
        {
            return await Task.FromResult(GetStatus(uploadId));
        }

        public UploadStatus GetStatus(string uploadId)
        {
            if (!_uploads.TryGetValue(uploadId, out var info) || !Directory.Exists(info.Folder))
                throw new KeyNotFoundException($"Upload {uploadId} not found");

            var parts = Directory.EnumerateFiles(info.Folder, "*.part")
                .Select(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                .OrderBy(i => i)
                .ToArray();

            long bytes = parts.Sum(i => new FileInfo(Path.Combine(info.Folder, $"{i}.part")).Length);

            // Update last activity
            info.LastActivity = DateTime.UtcNow;

            return new UploadStatus(parts, bytes, info.ChunkSize);
        }

        public async Task<string> CompleteAsync(string uploadId, int totalChunks, string? fileName, string? expectedSha256, CancellationToken cancellationToken = default)
        {
            await CompleteUploadAsync(uploadId, totalChunks, fileName, expectedSha256, cancellationToken);
            return Consume(uploadId);
        }

        public async Task CompleteUploadAsync(string uploadId, int totalChunks, string? fileName, string? expectedSha256, CancellationToken cancellationToken = default)
        {
            if (!_uploads.TryGetValue(uploadId, out var info))
                throw new KeyNotFoundException($"Upload {uploadId} not found");

            // Verify all chunks exist
            for (var i = 0; i < totalChunks; i++)
            {
                var partFile = Path.Combine(info.Folder, $"{i}.part");
                if (!File.Exists(partFile))
                    throw new InvalidOperationException($"Missing chunk {i}.");
            }

            var finalName = fileName ?? info.FileName ?? $"{uploadId}.bin";

            // Handle Arabic file names - decode URL-encoded names and keep original Arabic names
            if (!string.IsNullOrEmpty(finalName))
            {
                try
                {
                    // Check if the file name is URL-encoded (contains % characters)
                    if (finalName.Contains("%"))
                    {
                        _logger.LogInformation("UploadService: Detected URL-encoded file name: {FileName}", finalName);

                        // Decode the URL-encoded file name
                        var decodedName = WebUtility.UrlDecode(finalName);
                        if (decodedName != finalName)
                        {
                            _logger.LogInformation("UploadService: Decoded file name: {Original} -> {Decoded}", finalName, decodedName);
                            finalName = decodedName;
                        }
                    }

                    // Log the final file name (whether it's Arabic or ASCII)
                    if (finalName.Any(c => c > 127))
                    {
                        _logger.LogInformation("UploadService: File will be saved with Arabic name: {FileName}", finalName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "UploadService: Failed to process file name: {FileName}", finalName);
                    // Keep original name if processing fails
                }
            }

            var cfg = _configs.Get(info.SchemeName);
            if (string.IsNullOrWhiteSpace(cfg.StoragePath) || string.IsNullOrWhiteSpace(cfg.RequestPath))
                throw new InvalidOperationException($"Upload scheme '{info.SchemeName}' is not configured.");

            var storageRoot = Path.IsPathRooted(cfg.StoragePath)
                ? cfg.StoragePath
                : Path.Combine(_contentRoot, cfg.StoragePath);
            Directory.CreateDirectory(storageRoot);

            // Generate unique file name if file already exists
            finalName = await GenerateUniqueFileNameAsync(finalName, storageRoot);

            var finalPath = Path.Combine(info.Folder, finalName);
            var tempFinalPath = finalPath + ".tmp";

            try
            {
                SHA256? sha = string.IsNullOrWhiteSpace(expectedSha256) ? null : SHA256.Create();

                await using (var output = File.Create(tempFinalPath, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                    try
                    {
                        for (var i = 0; i < totalChunks; i++)
                        {
                            var partPath = Path.Combine(info.Folder, $"{i}.part");
                            await using var input = File.OpenRead(partPath);

                            int read;
                            while ((read = await input.ReadAsync(buffer, 0, BufferSize, cancellationToken)) > 0)
                            {
                                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                                sha?.TransformBlock(buffer, 0, read, null, 0);
                            }
                        }
                        sha?.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                // Verify SHA256 if provided
                if (sha != null)
                {
                    var computed = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
                    if (!computed.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(tempFinalPath);
                        throw new InvalidOperationException("SHA256 mismatch.");
                    }
                }

                // Atomic move to final location
                File.Move(tempFinalPath, finalPath, overwrite: true);

                // Clean up chunk files
                foreach (var file in Directory.EnumerateFiles(info.Folder, "*.part"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete chunk file {File}", file);
                    }
                }

                var destPath = Path.Combine(storageRoot, finalName);
                File.Move(finalPath, destPath, overwrite: true);

                _logger.LogInformation("UploadService: File saved successfully with name: {FileName} at path: {FilePath}", finalName, destPath);

                // Clean up upload folder
                try
                {
                    Directory.Delete(info.Folder, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete upload folder {Folder}", info.Folder);
                }

                var url = $"{cfg.RequestPath.TrimEnd('/')}/{finalName}";
                _uploads.TryRemove(uploadId, out _);
                _completed[uploadId] = url; // Store the URL for Consume method

                _logger.LogInformation("UploadService: File saved successfully with Arabic name: {FileName} at URL: {Url}", finalName, url);
            }
            catch (Exception ex)
            {
                // Cleanup temp file on error
                if (File.Exists(tempFinalPath))
                {
                    try { File.Delete(tempFinalPath); } catch { }
                }
                _logger.LogError(ex, "Failed to complete upload {UploadId}", uploadId);
                throw;
            }
        }

        public async Task<string> ConsumeAsync(string uploadId)
        {
            return await Task.FromResult(Consume(uploadId));
        }

        public async Task AbortAsync(string uploadId)
        {
            await Task.Run(() => Delete(uploadId));
        }

        public string Consume(string uploadId)
        {
            if (_completed.TryRemove(uploadId, out var url))
                return url;
            throw new KeyNotFoundException($"Completed upload {uploadId} not found");
        }

        public void Delete(string uploadId)
        {
            _uploads.TryRemove(uploadId, out var info);
            var folder = info?.Folder ?? Path.Combine(_root, uploadId);
            if (Directory.Exists(folder))
            {
                try
                {
                    Directory.Delete(folder, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete upload folder {Folder}", folder);
                }
            }
        }

        /// <summary>
        /// Generates a unique file name to prevent conflicts when multiple files have the same name.
        /// Example: "سيف.mp4" -> "سيف.mp4", "سيف_1.mp4", "سيف_2.mp4", etc.
        /// </summary>
        /// <param name="originalFileName">The original file name</param>
        /// <param name="storagePath">The storage directory path</param>
        /// <returns>A unique file name</returns>
        private async Task<string> GenerateUniqueFileNameAsync(string originalFileName, string storagePath)
        {
            if (string.IsNullOrEmpty(originalFileName))
                return Guid.NewGuid().ToString("N") + ".bin";

            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

            // Start with the original file name
            var candidateName = originalFileName;
            var counter = 0;

            // Check if file exists and increment counter until we find a unique name
            while (File.Exists(Path.Combine(storagePath, candidateName)))
            {
                counter++;
                candidateName = $"{nameWithoutExtension}_{counter}{extension}";

                // Safety check to prevent infinite loops
                if (counter > 10000)
                {
                    _logger.LogWarning("GenerateUniqueFileNameAsync: Counter exceeded 10000 for file {OriginalFileName}, using GUID", originalFileName);
                    candidateName = $"{nameWithoutExtension}_{Guid.NewGuid():N}{extension}";
                    break;
                }
            }

            if (candidateName != originalFileName)
            {
                _logger.LogInformation("GenerateUniqueFileNameAsync: Generated unique name: {Original} -> {Unique}", originalFileName, candidateName);
            }

            return candidateName;
        }
    }
}

using Application.DTOs;
using Application.Services;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;

namespace Infrastructure.Services
{
    public class DownloadService : IDownloadService
    {
        private readonly string _contentRoot;
        private readonly ConcurrentDictionary<string, DownloadSession> _downloads;
        private readonly IOptionsMonitor<FileSettings> _configs;
        private readonly ILogger<DownloadService> _logger;
        private const long DefaultChunkSize = 50L * 1024 * 1024; // 50MB
        private const int BufferSize = 64 * 1024; // 64KB buffer

        public DownloadService(
            IConfiguration config,
            IWebHostEnvironment env,
            ConcurrentDictionary<string, DownloadSession> downloads,
            ILogger<DownloadService> logger,
            IOptionsMonitor<FileSettings> configs)
        {
            _contentRoot = env.ContentRootPath;
            _downloads = downloads;
            _logger = logger;
            _configs = configs;
        }

        public async Task<DownloadInfo> InitiateAsync(string fileRequestPath, string? schemeName = null)
        {
            var (filePath, scheme) = await ResolveFilePathAsync(fileRequestPath, schemeName);
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {fileRequestPath}");

            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            var chunkSize = DefaultChunkSize;
            var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);

            var downloadId = Guid.NewGuid().ToString("N");
            var session = new DownloadSession
            {
                DownloadId = downloadId,
                FilePath = filePath,
                FileName = fileName,
                FileSize = fileInfo.Length,
                ChunkSize = chunkSize,
                TotalChunks = totalChunks,
                ContentType = GetContentType(fileName),
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            _downloads[downloadId] = session;

            _logger.LogInformation("DownloadService: Initiated download session {DownloadId} for file {FileName} ({FileSize} bytes, {TotalChunks} chunks)",
                downloadId, fileName, fileInfo.Length, totalChunks);

            return new DownloadInfo
            {
                DownloadId = downloadId,
                FileName = fileName,
                FileSize = fileInfo.Length,
                ChunkSize = chunkSize,
                TotalChunks = totalChunks,
                ContentType = session.ContentType
            };
        }

        public async Task<DownloadStatus> GetStatusAsync(string downloadId)
        {
            if (!_downloads.TryGetValue(downloadId, out var session))
                throw new KeyNotFoundException($"Download session {downloadId} not found");

            // Update last activity
            session.LastActivity = DateTime.UtcNow;

            // For now, return all chunks as available (in real implementation, you might track downloaded chunks)
            var downloadedChunks = Enumerable.Range(0, session.TotalChunks).ToArray();

            return new DownloadStatus
            {
                DownloadId = downloadId,
                FileName = session.FileName,
                FileSize = session.FileSize,
                ChunkSize = session.ChunkSize,
                TotalChunks = session.TotalChunks,
                DownloadedChunks = downloadedChunks
            };
        }

        public async Task<Stream> DownloadChunkAsync(string downloadId, int chunkIndex, CancellationToken cancellationToken = default)
        {
            if (!_downloads.TryGetValue(downloadId, out var session))
                throw new KeyNotFoundException($"Download session {downloadId} not found");

            if (chunkIndex < 0 || chunkIndex >= session.TotalChunks)
                throw new ArgumentOutOfRangeException(nameof(chunkIndex), $"Invalid chunk index: {chunkIndex}");

            session.LastActivity = DateTime.UtcNow;

            var filePath = session.FilePath;
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var startOffset = chunkIndex * session.ChunkSize;
            var endOffset = Math.Min(startOffset + session.ChunkSize - 1, session.FileSize - 1);
            var chunkLength = endOffset - startOffset + 1;

            _logger.LogInformation("DownloadService: Downloading chunk {ChunkIndex} for session {DownloadId} (offset: {StartOffset}, length: {ChunkLength})",
                chunkIndex, downloadId, startOffset, chunkLength);

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            fileStream.Seek(startOffset, SeekOrigin.Begin);

            // Return a stream that will close the file stream when disposed
            return new ChunkedFileStream(fileStream, chunkLength);
        }

        public async Task<FileDownloadResult> DownloadFileAsync(string fileRequestPath, string? schemeName = null, long? rangeStart = null, long? rangeEnd = null, CancellationToken cancellationToken = default)
        {
            var (filePath, scheme) = await ResolveFilePathAsync(fileRequestPath, schemeName);
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {fileRequestPath}");

            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            var fileSize = fileInfo.Length;

            // Handle range requests (HTTP Range header support)
            long start = 0;
            long end = fileSize - 1;
            bool isRangeRequest = false;

            if (rangeStart.HasValue || rangeEnd.HasValue)
            {
                isRangeRequest = true;
                start = rangeStart ?? 0;
                end = rangeEnd ?? fileSize - 1;

                // Validate range
                if (start < 0) start = 0;
                if (end >= fileSize) end = fileSize - 1;
                if (start > end)
                {
                    start = 0;
                    end = fileSize - 1;
                    isRangeRequest = false;
                }
            }

            var length = end - start + 1;
            var contentType = GetContentType(fileName);

            _logger.LogInformation("DownloadService: Downloading file {FileName} (size: {FileSize}, range: {Start}-{End}, length: {Length})",
                fileName, fileSize, start, end, length);

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            fileStream.Seek(start, SeekOrigin.Begin);

            return new FileDownloadResult
            {
                Stream = new RangeFileStream(fileStream, start, length),
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileSize,
                RangeStart = isRangeRequest ? start : null,
                RangeEnd = isRangeRequest ? end : null,
                IsRangeRequest = isRangeRequest
            };
        }

        public async Task<FileInfoDto> GetFileInfoAsync(string fileRequestPath, string? schemeName = null)
        {
            var (filePath, scheme) = await ResolveFilePathAsync(fileRequestPath, schemeName);
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {fileRequestPath}");

            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            var cfg = _configs.Get(scheme);

            return new FileInfoDto
            {
                FileName = fileName,
                FileSize = fileInfo.Length,
                ContentType = GetContentType(fileName),
                LastModified = fileInfo.LastWriteTimeUtc,
                FileUrl = $"{cfg.RequestPath.TrimEnd('/')}/{fileName}"
            };
        }

        private async Task<(string filePath, string scheme)> ResolveFilePathAsync(string fileRequestPath, string? schemeName)
        {
            // If scheme is provided, use it
            if (!string.IsNullOrWhiteSpace(schemeName))
            {
                var cfg = _configs.Get(schemeName.ToLowerInvariant());
                var storagePath = Path.IsPathRooted(cfg.StoragePath)
                    ? cfg.StoragePath
                    : Path.Combine(_contentRoot, cfg.StoragePath);
                var fileName = Path.GetFileName(fileRequestPath);
                var filePath = Path.Combine(storagePath, fileName);
                return (filePath, schemeName.ToLowerInvariant());
            }

            // Otherwise, try to resolve from request path
            var schemes = new[] { "image", "pdf", "pptx", "video", "any" };
            foreach (var scheme in schemes)
            {
                var cfg = _configs.Get(scheme);
                if (fileRequestPath.StartsWith(cfg.RequestPath!, StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = Path.GetFileName(fileRequestPath);
                    var storagePath = Path.IsPathRooted(cfg.StoragePath)
                        ? cfg.StoragePath
                        : Path.Combine(_contentRoot, cfg.StoragePath);
                    var filePath = Path.Combine(storagePath, fileName);
                    return (filePath, scheme);
                }
            }

            throw new InvalidOperationException($"Could not resolve file path: {fileRequestPath}");
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".mkv" => "video/x-matroska",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".ppt" or ".pptx" => "application/vnd.ms-powerpoint",
                _ => "application/octet-stream"
            };
        }
    }

    /// <summary>
    /// Download session information
    /// </summary>
    public class DownloadSession
    {
        public string DownloadId { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public long FileSize { get; set; }
        public long ChunkSize { get; set; }
        public int TotalChunks { get; set; }
        public string ContentType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }
    }

    /// <summary>
    /// Stream wrapper for chunked file reading
    /// </summary>
    internal class ChunkedFileStream : Stream
    {
        private readonly FileStream _fileStream;
        private readonly long _chunkLength;
        private long _bytesRead;

        public ChunkedFileStream(FileStream fileStream, long chunkLength)
        {
            _fileStream = fileStream;
            _chunkLength = chunkLength;
            _bytesRead = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _chunkLength;
        public override long Position
        {
            get => _bytesRead;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _chunkLength - _bytesRead;
            if (remaining <= 0) return 0;

            var toRead = (int)Math.Min(count, remaining);
            var read = _fileStream.Read(buffer, offset, toRead);
            _bytesRead += read;
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var remaining = _chunkLength - _bytesRead;
            if (remaining <= 0) return 0;

            var toRead = (int)Math.Min(count, remaining);
            var read = await _fileStream.ReadAsync(buffer, offset, toRead, cancellationToken);
            _bytesRead += read;
            return read;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileStream?.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// Stream wrapper for range requests
    /// </summary>
    internal class RangeFileStream : Stream
    {
        private readonly FileStream _fileStream;
        private readonly long _start;
        private readonly long _length;
        private long _bytesRead;

        public RangeFileStream(FileStream fileStream, long start, long length)
        {
            _fileStream = fileStream;
            _start = start;
            _length = length;
            _bytesRead = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position
        {
            get => _bytesRead;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _length - _bytesRead;
            if (remaining <= 0) return 0;

            var toRead = (int)Math.Min(count, remaining);
            var read = _fileStream.Read(buffer, offset, toRead);
            _bytesRead += read;
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var remaining = _length - _bytesRead;
            if (remaining <= 0) return 0;

            var toRead = (int)Math.Min(count, remaining);
            var read = await _fileStream.ReadAsync(buffer, offset, toRead, cancellationToken);
            _bytesRead += read;
            return read;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileStream?.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}


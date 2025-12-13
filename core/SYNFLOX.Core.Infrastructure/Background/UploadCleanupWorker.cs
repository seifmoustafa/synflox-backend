using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Infrastructure.Services;
using System.Linq;

namespace Infrastructure.Background
{
    public class UploadCleanupWorker : BackgroundService
    {
        private readonly string _root;
        private readonly ConcurrentDictionary<string, UploadInfo> _uploads;
        private readonly ILogger<UploadCleanupWorker> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30); // More frequent cleanup
        private readonly TimeSpan _uploadTimeout = TimeSpan.FromHours(24); // 24 hour timeout

        public UploadCleanupWorker(IConfiguration config, IWebHostEnvironment env, ConcurrentDictionary<string, UploadInfo> uploads, ILogger<UploadCleanupWorker> logger)
        {
            _root = config["UploadRoot"] ?? Path.Combine(env.ContentRootPath, "uploads");
            _uploads = uploads;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Upload cleanup worker started. Cleanup interval: {Interval}, Upload timeout: {Timeout}",
                _cleanupInterval, _uploadTimeout);

            var timer = new PeriodicTimer(_cleanupInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformCleanupAsync(stoppingToken);
            }
        }

        private Task PerformCleanupAsync(CancellationToken cancellationToken)
        {
            var cutoffTime = DateTime.UtcNow - _uploadTimeout;
            var cleanedCount = 0;
            var errorCount = 0;

            try
            {
                // Clean up uploads based on last activity
                var uploadsToRemove = _uploads
                    .Where(kvp => kvp.Value.LastActivity < cutoffTime)
                    .ToList();

                foreach (var (uploadId, info) in uploadsToRemove)
                {
                    try
                    {
                        _uploads.TryRemove(uploadId, out _);
                        if (Directory.Exists(info.Folder))
                        {
                            Directory.Delete(info.Folder, true);
                            _logger.LogDebug("Cleaned up inactive upload {UploadId} (last activity: {LastActivity})",
                                uploadId, info.LastActivity);
                        }
                        cleanedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to clean up upload {UploadId}", uploadId);
                        errorCount++;
                    }
                }

                // Clean up orphaned directories (not in _uploads dictionary)
                if (Directory.Exists(_root))
                {
                    foreach (var dir in Directory.EnumerateDirectories(_root))
                    {
                        try
                        {
                            var dirName = Path.GetFileName(dir);
                            if (!_uploads.ContainsKey(dirName))
                            {
                                var lastWrite = Directory.GetLastWriteTimeUtc(dir);
                                if (lastWrite < cutoffTime)
                                {
                                    Directory.Delete(dir, true);
                                    _logger.LogDebug("Cleaned up orphaned directory {Dir} (last write: {LastWrite})",
                                        dir, lastWrite);
                                    cleanedCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to clean up directory {Dir}", dir);
                            errorCount++;
                        }
                    }
                }

                if (cleanedCount > 0 || errorCount > 0)
                {
                    _logger.LogInformation("Cleanup completed: {Cleaned} items cleaned, {Errors} errors",
                        cleanedCount, errorCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during upload cleanup");
            }

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Upload cleanup worker stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}

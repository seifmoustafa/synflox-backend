using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services;

public class UploadCleanupService : BackgroundService
{
    private readonly string _uploadsRoot;
    private readonly ILogger<UploadCleanupService> _logger;
    private static readonly TimeSpan Expiration = TimeSpan.FromHours(48);

    public UploadCleanupService(IWebHostEnvironment env, ILogger<UploadCleanupService> logger)
    {
        _uploadsRoot = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_uploadsRoot);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (Directory.Exists(_uploadsRoot))
                {
                    foreach (var dir in Directory.EnumerateDirectories(_uploadsRoot))
                    {
                        var lastWrite = Directory.GetLastWriteTimeUtc(dir);
                        if (DateTime.UtcNow - lastWrite > Expiration)
                        {
                            Directory.Delete(dir, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning uploads");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

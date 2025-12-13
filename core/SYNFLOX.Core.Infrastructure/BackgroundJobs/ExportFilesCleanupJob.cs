using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Background service to cleanup expired export files every 5 minutes
    /// </summary>
    public class ExportFilesCleanupJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExportFilesCleanupJob> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public ExportFilesCleanupJob(
            IServiceProvider serviceProvider,
            ILogger<ExportFilesCleanupJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Export Files Cleanup Job started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var cleanupService = scope.ServiceProvider
                        .GetRequiredService<IFileHostExportService>();

                    await cleanupService.CleanupExpiredFilesAsync();
                }
                catch (OperationCanceledException)
                {
                    // Expected when application is shutting down
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Export Files Cleanup Job");
                    // Continue running even if cleanup fails
                }
            }

            _logger.LogInformation("Export Files Cleanup Job stopped");
        }
    }
}

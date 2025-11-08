using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.HealthChecks;

/// <summary>
/// Health check for disk space availability.
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private const long MinimumFreeSpaceGB = 1; // Minimum 1 GB free space required

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the drive where the application is running
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var driveInfo = new DriveInfo(Path.GetPathRoot(appPath) ?? appPath);

            var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var totalSpaceGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
            var usedSpaceGB = totalSpaceGB - freeSpaceGB;
            var freeSpacePercent = (freeSpaceGB / totalSpaceGB) * 100;

            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "FreeSpaceGB", Math.Round(freeSpaceGB, 2) },
                { "TotalSpaceGB", Math.Round(totalSpaceGB, 2) },
                { "UsedSpaceGB", Math.Round(usedSpaceGB, 2) },
                { "FreeSpacePercent", Math.Round(freeSpacePercent, 2) },
                { "DriveName", driveInfo.Name }
            };

            if (freeSpaceGB < MinimumFreeSpaceGB)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Low disk space: {Math.Round(freeSpaceGB, 2)} GB available (minimum: {MinimumFreeSpaceGB} GB)",
                    data: data));
            }

            if (freeSpacePercent < 10)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {Math.Round(freeSpacePercent, 2)}% free",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space is healthy: {Math.Round(freeSpaceGB, 2)} GB available ({Math.Round(freeSpacePercent, 2)}% free)",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space health check failed", ex));
        }
    }
}




using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Configurations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Infrastructure.HealthChecks;

/// <summary>
/// Health check for SMTP server connectivity.
/// </summary>
public class EmailHealthCheck : IHealthCheck
{
    private readonly EmailSettings _settings;

    public EmailHealthCheck(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if SMTP settings are configured
            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.User) ||
                string.IsNullOrWhiteSpace(_settings.Pass))
            {
                return HealthCheckResult.Degraded("SMTP settings are not fully configured");
            }

            // Try to connect to SMTP server (TCP connection test)
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(_settings.Host, _settings.Port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                return HealthCheckResult.Unhealthy($"SMTP server {_settings.Host}:{_settings.Port} is not reachable (timeout)");
            }

            if (tcpClient.Connected)
            {
                tcpClient.Close();
                return HealthCheckResult.Healthy($"SMTP server {_settings.Host}:{_settings.Port} is reachable");
            }

            return HealthCheckResult.Unhealthy($"SMTP server {_settings.Host}:{_settings.Port} connection failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"SMTP health check failed: {ex.Message}", ex);
        }
    }
}




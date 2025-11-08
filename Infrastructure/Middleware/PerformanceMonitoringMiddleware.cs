using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Middleware;

/// <summary>
/// Middleware that monitors request/response performance and logs slow queries.
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private const long SlowQueryThresholdMs = 1000; // 1 second

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip performance monitoring for certain paths
        if (path.StartsWith("/swagger") ||
            path.StartsWith("/api/health") ||
            path.StartsWith("/api/metrics"))
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Log slow queries
            if (elapsedMs > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                    context.Request.Method,
                    path,
                    elapsedMs,
                    context.Response.StatusCode);
            }

            // Record performance metric
            try
            {
                var metricsService = context.RequestServices.GetService<IMetricsService>();
                if (metricsService != null)
                {
                    // Record response time metric
                    var tags = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        endpoint = path,
                        method = context.Request.Method,
                        statusCode = context.Response.StatusCode
                    });

                    await metricsService.RecordMetricAsync(
                        Domain.Enums.MetricType.ResponseTime,
                        elapsedMs,
                        tags);
                }
            }
            catch (Exception ex)
            {
                // Don't let metrics recording break the request
                _logger.LogWarning(ex, "Error recording performance metric");
            }
        }
    }
}




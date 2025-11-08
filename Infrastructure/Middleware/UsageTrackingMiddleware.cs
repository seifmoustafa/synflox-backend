using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Middleware;

/// <summary>
/// Middleware that tracks API usage for analytics.
/// </summary>
public class UsageTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public UsageTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tracking for certain paths
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/swagger") ||
            path.StartsWith("/api/health") ||
            path.StartsWith("/api/authentication"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Get company ID from context (set by ApiKeyAuthenticationMiddleware or JWT)
            var companyId = context.Items["CompanyId"] as Guid?;
            if (companyId.HasValue)
            {
                try
                {
                    var analyticsService = context.RequestServices.GetRequiredService<ICompanyUsageAnalyticsService>();
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                    var userAgent = context.Request.Headers["User-Agent"].ToString();
                    var apiKeyId = context.Items["ApiKeyId"] as Guid?;

                    await analyticsService.LogUsageAsync(
                        companyId.Value,
                        context.Request.Path.Value ?? "",
                        context.Request.Method,
                        stopwatch.ElapsedMilliseconds,
                        context.Response.StatusCode,
                        ipAddress,
                        userAgent,
                        apiKeyId);
                }
                catch
                {
                    // Don't fail the request if logging fails
                }
            }
        }
    }
}


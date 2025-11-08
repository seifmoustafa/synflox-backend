using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Application.Services;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

namespace Infrastructure.Middleware;

/// <summary>
/// Middleware that implements rate limiting for API requests.
/// Supports global limits, per-endpoint limits, and per-API-key limits.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitingSettings _settings;
    private readonly IDistributedCache _cache;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<RateLimitingSettings> settings,
        IDistributedCache cache)
    {
        _next = next;
        _settings = settings.Value;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_settings.Enabled)
        {
            await _next(context);
            return;
        }

        // Skip rate limiting for certain paths
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/swagger") ||
            path.StartsWith("/api/health"))
        {
            await _next(context);
            return;
        }

        var now = DateTime.UtcNow;
        var hourKey = now.ToString("yyyy-MM-dd-HH");

        // Get API key from context (set by ApiKeyAuthenticationMiddleware)
        var apiKey = context.Items["ApiKey"] as Application.DTOs.Authentication.ApiKeyDto;
        var apiKeyId = apiKey?.Id.ToString() ?? "anonymous";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Build cache keys
        var globalKey = $"ratelimit:global:{hourKey}";
        var endpointKey = GetEndpointKey(path, hourKey);
        var apiKeyLimitKey = apiKey != null ? $"ratelimit:apikey:{apiKeyId}:{hourKey}" : null;

        // Check global limit
        var globalCount = await GetCountAsync(globalKey);
        if (globalCount >= _settings.GlobalLimitPerHour)
        {
            await SetRateLimitHeaders(context, _settings.GlobalLimitPerHour, globalCount, now);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsync("Global rate limit exceeded");
            return;
        }

        // Check endpoint-specific limit
        if (endpointKey != null && _settings.EndpointLimits.TryGetValue(endpointKey, out var endpointLimit))
        {
            var endpointCount = await GetCountAsync($"ratelimit:endpoint:{endpointKey}:{hourKey}");
            if (endpointCount >= endpointLimit)
            {
                await SetRateLimitHeaders(context, endpointLimit, endpointCount, now);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("Endpoint rate limit exceeded");
                return;
            }
            await IncrementCountAsync($"ratelimit:endpoint:{endpointKey}:{hourKey}", endpointLimit);
        }

        // Check API key limit
        if (apiKeyLimitKey != null)
        {
            var apiKeyLimit = apiKey.RateLimitPerHour ?? _settings.DefaultApiKeyLimitPerHour;
            var apiKeyCount = await GetCountAsync(apiKeyLimitKey);
            if (apiKeyCount >= apiKeyLimit)
            {
                await SetRateLimitHeaders(context, apiKeyLimit, apiKeyCount, now);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("API key rate limit exceeded");
                return;
            }
            await IncrementCountAsync(apiKeyLimitKey, apiKeyLimit);
        }

        // Increment counters
        await IncrementCountAsync(globalKey, _settings.GlobalLimitPerHour);

        // Set rate limit headers
        await SetRateLimitHeaders(context, _settings.GlobalLimitPerHour, globalCount + 1, now);

        await _next(context);
    }

    private string? GetEndpointKey(string path, string hourKey)
    {
        // Try to match endpoint patterns from settings
        foreach (var pattern in _settings.EndpointLimits.Keys)
        {
            // Convert pattern to regex (e.g., "/api/licensing/{id}/status" -> "/api/licensing/[^/]+/status")
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\{id\\}", "[^/]+") + "$";
            if (Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase))
            {
                return pattern;
            }
        }
        return null;
    }

    private async Task<int> GetCountAsync(string key)
    {
        try
        {
            var value = await _cache.GetStringAsync(key);
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }
        catch
        {
            return 0;
        }
    }

    private async Task IncrementCountAsync(string key, int limit)
    {
        try
        {
            var current = await GetCountAsync(key);
            var newValue = current + 1;
            
            // Store with expiration (until end of current hour)
            var now = DateTime.UtcNow;
            var expiresAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
            var expiration = expiresAt - now;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, newValue.ToString(), options);
        }
        catch
        {
            // Ignore cache errors
        }
    }

    private async Task SetRateLimitHeaders(HttpContext context, int limit, int remaining, DateTime resetTime)
    {
        var resetAt = new DateTime(resetTime.Year, resetTime.Month, resetTime.Day, resetTime.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
        var resetSeconds = (int)(resetAt - DateTime.UtcNow).TotalSeconds;

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - remaining).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetSeconds.ToString();
    }
}


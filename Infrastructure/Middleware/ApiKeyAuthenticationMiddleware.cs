using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Middleware;

/// <summary>
/// Middleware that authenticates requests using API keys.
/// Validates API key from Authorization header: "Bearer {api_key}"
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key authentication for certain paths (but still allow tracking if API key is provided)
        // Note: /api/licensing/validate-key is NOT skipped - API keys will be validated if provided for tracking
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var skipAuth = path.StartsWith("/api/auth/") || 
                       path.StartsWith("/api/health") ||
                       path.StartsWith("/swagger");

        // Check if request has API key in Authorization header
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.ToString();
            if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = authValue.Substring("Bearer ".Length).Trim();
                
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    var apiKeyService = context.RequestServices.GetRequiredService<IApiKeyService>();
                    var (isValid, companyId, apiKeyDto) = await apiKeyService.ValidateApiKeyAsync(apiKey);

                    if (isValid && apiKeyDto != null)
                    {
                        // Check IP whitelist if configured
                        if (!IsIpAllowed(apiKeyDto.AllowedIps, context.Connection.RemoteIpAddress))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            await context.Response.WriteAsync("IP address not allowed");
                            return;
                        }

                        // Check expiration
                        if (apiKeyDto.ExpiresAt.HasValue && apiKeyDto.ExpiresAt.Value < DateTime.UtcNow)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            await context.Response.WriteAsync("API key has expired");
                            return;
                        }

                        // Add company ID to context for downstream use (for tracking)
                        context.Items["CompanyId"] = companyId;
                        context.Items["ApiKeyId"] = apiKeyDto.Id;
                        context.Items["ApiKey"] = apiKeyDto;

                        // Continue to next middleware
                        await _next(context);
                        return;
                    }
                }
            }
        }

        // If skipped paths, continue without API key validation
        if (skipAuth)
        {
            await _next(context);
            return;
        }

        // If no valid API key, check if endpoint requires authentication
        // For now, we allow requests without API key to continue (JWT auth might handle it)
        // In production, you might want to require API key for certain endpoints
        await _next(context);
    }

    private static bool IsIpAllowed(string[]? allowedIps, IPAddress? requestIp)
    {
        // If no IP restrictions, allow from anywhere
        if (allowedIps == null || allowedIps.Length == 0)
        {
            return true;
        }

        if (requestIp == null)
        {
            return false;
        }

        var requestIpString = requestIp.ToString();

        // Check if request IP matches any allowed IP
        return allowedIps.Any(allowedIp =>
        {
            // Support CIDR notation (e.g., "192.168.1.0/24")
            if (allowedIp.Contains('/'))
            {
                try
                {
                    var parts = allowedIp.Split('/');
                    var networkIp = IPAddress.Parse(parts[0]);
                    var prefixLength = int.Parse(parts[1]);

                    if (networkIp.AddressFamily == requestIp.AddressFamily)
                    {
                        var networkBytes = networkIp.GetAddressBytes();
                        var requestBytes = requestIp.GetAddressBytes();
                        var bytesToCheck = prefixLength / 8;
                        var bitsToCheck = prefixLength % 8;

                        for (int i = 0; i < bytesToCheck; i++)
                        {
                            if (networkBytes[i] != requestBytes[i])
                                return false;
                        }

                        if (bitsToCheck > 0)
                        {
                            var mask = (byte)(0xFF << (8 - bitsToCheck));
                            if ((networkBytes[bytesToCheck] & mask) != (requestBytes[bytesToCheck] & mask))
                                return false;
                        }

                        return true;
                    }
                }
                catch
                {
                    // Invalid CIDR, fall through to exact match
                }
            }

            // Exact match
            return allowedIp.Equals(requestIpString, StringComparison.OrdinalIgnoreCase);
        });
    }
}


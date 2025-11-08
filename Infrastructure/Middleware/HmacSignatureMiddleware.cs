using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.Services;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Middleware;

/// <summary>
/// Middleware that validates HMAC signatures for API key authenticated requests.
/// </summary>
public class HmacSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HmacSignatureMiddleware> _logger;

    public HmacSignatureMiddleware(
        RequestDelegate next,
        ILogger<HmacSignatureMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate HMAC for API key authenticated requests
        // Get API key from Authorization header (Bearer token) or X-Api-Key header
        string? apiKeyValue = null;
        
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.ToString();
            if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                apiKeyValue = authValue.Substring("Bearer ".Length).Trim();
            }
        }
        
        if (string.IsNullOrWhiteSpace(apiKeyValue) && context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
        {
            apiKeyValue = apiKeyHeader.ToString();
        }
        
        if (string.IsNullOrWhiteSpace(apiKeyValue))
        {
            await _next(context);
            return;
        }

        // Check if X-Signature header is present
        if (!context.Request.Headers.ContainsKey("X-Signature"))
        {
            // HMAC signature is optional for API key requests
            // If not provided, proceed without validation
            await _next(context);
            return;
        }

        var signature = context.Request.Headers["X-Signature"].ToString();
        var timestamp = context.Request.Headers["X-Timestamp"].ToString();

        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestamp))
        {
            _logger.LogWarning("HMAC signature validation skipped: Missing signature or timestamp header");
            await _next(context);
            return;
        }

        // Validate timestamp (prevent replay attacks)
        if (!long.TryParse(timestamp, out var timestampLong))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid timestamp format");
            return;
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestampLong).UtcDateTime;
        var now = DateTime.UtcNow;
        var timeDifference = Math.Abs((now - requestTime).TotalMinutes);

        // Allow 5-minute window for clock skew
        if (timeDifference > 5)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Request timestamp is too old or too far in the future");
            return;
        }

        // Get API key from database by hashing the provided key
        // Resolve scoped service from request services (not from constructor)
        var apiKeyRepository = context.RequestServices.GetRequiredService<IApiKeyRepository>();
        var apiKeys = await apiKeyRepository.FindAsync(ak => !ak.IsDeleted);
        Domain.Entities.Authentication.ApiKey? apiKey = null;
        
        var providedKeyHash = HashApiKey(apiKeyValue);
        foreach (var key in apiKeys)
        {
            // Compare hashed key with stored hash
            if (providedKeyHash == key.KeyHash)
            {
                apiKey = key;
                break;
            }
        }

        if (apiKey == null || string.IsNullOrWhiteSpace(apiKey.SigningSecret))
        {
            _logger.LogWarning("HMAC signature validation skipped: API key not found or no signing secret");
            await _next(context);
            return;
        }

        // Build the string to sign
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var queryString = context.Request.QueryString.Value ?? string.Empty;
        
        // Read request body
        string body = string.Empty;
        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        var stringToSign = $"{timestamp}{method}{path}{queryString}{body}";

        // Compute HMAC SHA256
        var computedSignature = ComputeHmacSha256(stringToSign, apiKey.SigningSecret);

        // Compare signatures (constant-time comparison to prevent timing attacks)
        if (!ConstantTimeEquals(computedSignature, signature))
        {
            _logger.LogWarning("HMAC signature validation failed for API key {ApiKeyId}", apiKey.Id);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid request signature");
            return;
        }

        _logger.LogDebug("HMAC signature validation successful for API key {ApiKeyId}", apiKey.Id);
        await _next(context);
    }

    private string ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }

    private string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }

    private bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}


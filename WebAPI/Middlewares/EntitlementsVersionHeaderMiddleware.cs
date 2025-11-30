using System.Threading.Tasks;
using Application.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebAPI.Middlewares;

/// <summary>
/// Middleware that adds X-Entitlements-Version header to all client API responses
/// Clients use this header to detect when their cached entitlements are stale
/// 
/// Best Practice: Get version BEFORE response, add header synchronously in OnStarting
/// </summary>
public class EntitlementsVersionHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EntitlementsVersionHeaderMiddleware> _logger;

    public EntitlementsVersionHeaderMiddleware(
        RequestDelegate next, 
        ILogger<EntitlementsVersionHeaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ClientJwtService jwtService, IEntitlementService entitlementService)
    {
        // Only apply to client API endpoints
        if (!context.Request.Path.StartsWithSegments("/api/client"))
        {
            await _next(context);
            return;
        }

        // Get subscription ID from claims (after authentication middleware runs)
        var subscriptionId = jwtService.GetSubscriptionId(context.User);
        
        // If no valid subscription, skip version header
        if (!subscriptionId.HasValue)
        {
            await _next(context);
            return;
        }

        // Get version BEFORE response starts (efficient - single DB call)
        int? version = null;
        try
        {
            version = await entitlementService.GetEntitlementsVersionAsync(subscriptionId.Value);
        }
        catch
        {
            // Don't fail the request if we can't get the version
        }

        // Register synchronous callback to add header
        if (version.HasValue)
        {
            context.Response.OnStarting(() =>
            {
                // Skip if already added (e.g., by the entitlements endpoint itself)
                if (!context.Response.Headers.ContainsKey("X-Entitlements-Version"))
                {
                    context.Response.Headers.Append("X-Entitlements-Version", version.Value.ToString());
                    _logger.LogDebug("Added X-Entitlements-Version header: {Version} for subscription {SubscriptionId}", 
                        version, subscriptionId);
                }
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method for registering the middleware
/// </summary>
public static class EntitlementsVersionHeaderMiddlewareExtensions
{
    public static IApplicationBuilder UseEntitlementsVersionHeader(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EntitlementsVersionHeaderMiddleware>();
    }
}

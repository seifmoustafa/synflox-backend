using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebAPI.Middlewares;

/// <summary>
/// Middleware for authenticating client access tokens
/// Validates JWT tokens and sets up the security context for client API requests
/// </summary>
public class ClientAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ClientJwtService _jwtService;
    private readonly ILogger<ClientAuthenticationMiddleware> _logger;

    public ClientAuthenticationMiddleware(
        RequestDelegate next,
        ClientJwtService jwtService,
        ILogger<ClientAuthenticationMiddleware> logger)
    {
        _next = next;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process client API requests
        if (context.Request.Path.StartsWithSegments("/api/client"))
        {
            await ProcessClientAuthenticationAsync(context);
        }

        await _next(context);
    }

    private async Task ProcessClientAuthenticationAsync(HttpContext context)
    {
        try
        {
            // Skip authentication for anonymous endpoints
            if (IsAnonymousEndpoint(context.Request.Path))
            {
                return;
            }

            // Extract token from Authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Missing or invalid Authorization header for client API request");
                await WriteUnauthorizedResponse(context, "Missing or invalid Authorization header");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            var (isValid, principal, error) = _jwtService.ValidateToken(token);
            
            if (!isValid || principal == null)
            {
                _logger.LogWarning("Invalid client token for path {Path}: {Error}", context.Request.Path, error);
                await WriteUnauthorizedResponse(context, error ?? "Invalid token");
                return;
            }

            // Check endpoint access permissions
            var hasAccess = _jwtService.HasEndpointAccess(principal, context.Request.Path);
            
            if (!hasAccess)
            {
                _logger.LogWarning("Client token does not have access to endpoint {Path}", context.Request.Path);
                await WriteForbiddenResponse(context, "Access denied to this endpoint");
                return;
            }

            // Set the authenticated principal
            context.User = principal;

            // Add client-specific claims to context for easy access
            var tokenId = _jwtService.GetClientTokenId(principal);
            var companyId = _jwtService.GetCompanyId(principal);
            var subscriptionId = _jwtService.GetSubscriptionId(principal);

            if (tokenId.HasValue)
                context.Items["ClientTokenId"] = tokenId.Value;
            if (companyId.HasValue)
                context.Items["CompanyId"] = companyId.Value;
            if (subscriptionId.HasValue)
                context.Items["SubscriptionId"] = subscriptionId.Value;

            _logger.LogDebug("Client authenticated successfully for {Path}. Company: {CompanyId}, Token: {TokenId}", 
                context.Request.Path, companyId, tokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client authentication for {Path}", context.Request.Path);
            await WriteUnauthorizedResponse(context, "Authentication error");
        }
    }

    private static bool IsAnonymousEndpoint(PathString path)
    {
        var anonymousEndpoints = new[]
        {
            "/api/client/auth/validate-token",
            "/api/client/docs"
        };

        return anonymousEndpoints.Any(endpoint => path.StartsWithSegments(endpoint));
    }

    private static string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader))
            return null;

        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            success = false,
            message = message,
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.Value
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    private static async Task WriteForbiddenResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            success = false,
            message = message,
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.Value
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}

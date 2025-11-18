using Application.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.RateLimiting;

namespace WebAPI.Configurations
{
    public static class RateLimiterConfiguration
    {
        public static IServiceCollection AddAuthenticationRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    // Exempt password reset endpoints - they have their own per-user rate limiting
                    var path = context.Request.Path.ToString().ToLower();
                    if (path.Contains("/forgot-password") || 
                        path.Contains("/verify-reset-otp") || 
                        path.Contains("/reset-password") ||
                        path.Contains("/validate-magic-link"))
                    {
                        return RateLimitPartition.GetNoLimiter("exempt");
                    }

                    // Get the most specific client IP address available
                    // Priority: X-Forwarded-For (real client IP) > X-Real-IP > Connection IP
                    string clientIp;
                    
                    // Check if behind reverse proxy (nginx, load balancer, etc.)
                    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        // X-Forwarded-For can contain multiple IPs: "client, proxy1, proxy2"
                        // Take the first one (original client)
                        clientIp = forwardedFor.Split(',')[0].Trim();
                    }
                    else
                    {
                        // Check X-Real-IP header (some proxies use this)
                        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                        clientIp = !string.IsNullOrEmpty(realIp) 
                            ? realIp 
                            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    }

                    // Rate limit per client IP (per device)
                    // Same device = same IP = same limit (regardless of browser)
                    return RateLimitPartition.GetFixedWindowLimiter(
                        clientIp,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10000,  // 10000 requests per minute per device (DEV MODE - very high limit)
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("RateLimiter");
                    
                    // Get the same client IP logic for logging
                    var forwardedFor = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    string clientIp;
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        clientIp = forwardedFor.Split(',')[0].Trim();
                    }
                    else
                    {
                        var realIp = context.HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
                        clientIp = !string.IsNullOrEmpty(realIp) 
                            ? realIp 
                            : context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    }
                    
                    logger.LogWarning("Rate limit exceeded for client IP: {ClientIP}", clientIp);

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    var response = new ApiResponse<string>(
                        StatusCodes.Status429TooManyRequests,
                        "Too many requests. Please try again later.");
                    await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: token);
                };
            });
            return services;
        }
    }
}

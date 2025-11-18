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

                    // Create device fingerprint: IP + User-Agent
                    // This ensures each device gets its own rate limit, even on same network
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var userAgent = context.Request.Headers.UserAgent.ToString();
                    
                    // Create a unique identifier for this device
                    // Use first 50 chars of user agent to avoid extremely long keys
                    var deviceFingerprint = $"{ipAddress}:{(string.IsNullOrEmpty(userAgent) ? "no-agent" : userAgent.Substring(0, Math.Min(50, userAgent.Length)))}";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        deviceFingerprint,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,  // Increased from 5 to 100 requests per minute per device
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
                    
                    var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var userAgent = context.HttpContext.Request.Headers.UserAgent.ToString();
                    var deviceInfo = string.IsNullOrEmpty(userAgent) ? "no-agent" : userAgent.Substring(0, Math.Min(50, userAgent.Length));
                    
                    logger.LogWarning("Rate limit exceeded for device: IP={IP}, UserAgent={UserAgent}", ip, deviceInfo);

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

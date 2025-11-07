using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebAPI.Middlewares;

/// <summary>
/// Optimized request logging middleware that only logs essential information for performance.
/// Skips body buffering for upload endpoints and minimizes logging overhead.
/// </summary>
public class RequestLoggingMiddleware : IMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private static readonly string[] SkipBodyPaths =
    {
        "/api/uploads/chunk",
        "/api/uploads/initiate",
        "/api/uploads/complete",
        "/api/uploads/status",
        "/api/uploads/progress",
    };
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var user = context.User?.Identity?.IsAuthenticated == true
            ? context.User.Identity.Name
            : "anonymous";

        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
                && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using var scope = _logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        var shouldSkipBodyBuffering = SkipBodyPaths.Any(path =>
            context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase));

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Only log essential information for performance
            var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(logLevel,
                "{method} {path} responded {statusCode} in {elapsed}ms (user: {user})",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                user);
        }
    }
}

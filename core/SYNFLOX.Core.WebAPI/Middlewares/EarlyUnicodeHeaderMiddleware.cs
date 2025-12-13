using System.Text;

namespace WebAPI.Middlewares;

/// <summary>
/// Early middleware to handle non-ASCII characters in HTTP headers before Kestrel processes them
/// </summary>
public class EarlyUnicodeHeaderMiddleware : IMiddleware
{
    private readonly ILogger<EarlyUnicodeHeaderMiddleware> _logger;

    public EarlyUnicodeHeaderMiddleware(ILogger<EarlyUnicodeHeaderMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            // Check if this is an upload completion request
            if (context.Request.Path.StartsWithSegments("/api/uploads") &&
                context.Request.Method == "POST" &&
                context.Request.Path.Value?.Contains("/complete") == true)
            {
                _logger.LogInformation("EarlyUnicodeHeaderMiddleware: Processing upload completion request");

                // Check for non-ASCII characters in headers
                var headersToModify = new Dictionary<string, string>();
                foreach (var header in context.Request.Headers)
                {
                    var hasNonAscii = header.Key.Any(c => c > 127) || header.Value.Any(v => v != null && v.Any(c => c > 127));

                    if (hasNonAscii)
                    {
                        _logger.LogWarning("EarlyUnicodeHeaderMiddleware: Found non-ASCII in header {HeaderName}: {HeaderValue}",
                            header.Key, string.Join(", ", header.Value));

                        // For now, just log the issue - we can't modify headers at this point
                        // The real solution is to fix the frontend to not send Arabic characters in headers
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EarlyUnicodeHeaderMiddleware: Error processing request");
        }

        await next(context);
    }
}

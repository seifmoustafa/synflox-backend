using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebAPI.Middlewares;

/// <summary>
/// Adds caching headers to successful GET responses so that downstream CDNs can cache them.
/// Non-cacheable responses are explicitly marked as <c>no-store</c>.
/// </summary>
public class CacheHeadersMiddleware : IMiddleware
{
    private readonly CacheHeadersOptions _options;
    private readonly ILogger<CacheHeadersMiddleware> _logger;

    public CacheHeadersMiddleware(
        IOptions<CacheHeadersOptions> options,
        ILogger<CacheHeadersMiddleware> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.OnStarting(() =>
        {
            if (HttpMethods.IsGet(context.Request.Method) &&
                (context.Response.StatusCode is StatusCodes.Status200OK or StatusCodes.Status304NotModified) &&
                context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.Headers["Cache-Control"] = $"public,max-age={_options.MaxAgeSeconds}";
                if (_options.VaryByHeaders?.Length > 0)
                {
                    context.Response.Headers["Vary"] = string.Join(',', _options.VaryByHeaders);
                }
                _logger.LogDebug(
                    "Applied public cache headers for {Path}{Query}",
                    context.Request.Path,
                    context.Request.QueryString);
            }
            else
            {
                context.Response.Headers["Cache-Control"] = "no-store,max-age=0";
                _logger.LogDebug(
                    "Applied no-store cache headers for {Path}{Query}",
                    context.Request.Path,
                    context.Request.QueryString);
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}

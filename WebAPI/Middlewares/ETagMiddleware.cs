using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Infrastructure.Authentication;

namespace WebAPI.Middlewares;

/// <summary>
/// Adds ETag headers and serves cached responses for repeated GET requests.
/// Responds with 304 when the client's <c>If-None-Match</c> header matches
/// the current resource representation.
/// </summary>
public class ETagMiddleware : IMiddleware
{
    private readonly IDistributedCache? _distributedCache;
    private readonly IMemoryCache? _memoryCache;
    private readonly ETagOptions _options;
    private readonly ILogger<ETagMiddleware> _logger;
    private static CancellationTokenSource _resetCacheToken = new();
    private readonly bool _useDistributedCache;

    public ETagMiddleware(
        IDistributedCache? distributedCache,
        IMemoryCache? memoryCache,
        IOptions<ETagOptions> options,
        ILogger<ETagMiddleware> logger)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
        _useDistributedCache = distributedCache != null;
        
        if (!_useDistributedCache && _memoryCache == null)
        {
            throw new InvalidOperationException("Either distributed cache or memory cache must be registered");
        }
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var cacheKey = context.Request.Path + context.Request.QueryString;

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await next(context);
            ResetCache();
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = (context.User as CustomClaimsPrincipal)?.UserId.ToString() ?? string.Empty;
            cacheKey = $"{userId}:{cacheKey}";
        }
        var requestETag = context.Request.Headers[HeaderNames.IfNoneMatch];
        
        // Try to get from cache
        CachedResponse? cached = null;
        if (_useDistributedCache)
        {
            var cachedBytes = await _distributedCache.GetAsync(cacheKey);
            if (cachedBytes != null)
            {
                cached = System.Text.Json.JsonSerializer.Deserialize<CachedResponse>(
                    System.Text.Encoding.UTF8.GetString(cachedBytes));
            }
        }
        else if (_memoryCache != null && _memoryCache.TryGetValue(cacheKey, out cached))
        {
            // Memory cache fallback
        }
        
        if (cached is not null)
        {
            if (requestETag == cached.ETag)
            {
                _logger.LogInformation(
                    "Cache validated for {Key}; returning 304",
                    cacheKey);
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.Headers[HeaderNames.ETag] = cached.ETag;
                return;
            }

            _logger.LogInformation(
                "Cache hit for {Key}; serving cached response",
                cacheKey);
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.Headers[HeaderNames.ETag] = cached.ETag;
            context.Response.ContentType = cached.ContentType;
            context.Response.ContentLength = cached.Body.Length;
            await context.Response.BodyWriter.WriteAsync(cached.Body);
            return;
        }

        var originalBody = context.Response.Body;
        await using var ms = new MemoryStream();
        context.Response.Body = ms;

        await next(context);

        if (IsCacheable(context))
        {
            var bodyBytes = ms.ToArray();
            var etag = ComputeETag(bodyBytes);
            context.Response.Headers[HeaderNames.ETag] = etag;
            
            var cachedResponse = new CachedResponse(etag, bodyBytes, context.Response.ContentType ?? string.Empty);
            
            if (_useDistributedCache)
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.MaxAgeSeconds)
                };
                var serialized = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(cachedResponse);
                await _distributedCache.SetAsync(cacheKey, serialized, options);
            }
            else if (_memoryCache != null)
            {
                _memoryCache.Set(
                    cacheKey,
                    cachedResponse,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.MaxAgeSeconds),
                        ExpirationTokens = { new CancellationChangeToken(_resetCacheToken.Token) }
                    });
            }
            
            _logger.LogInformation(
                "Cached response for {Key} with ETag {ETag}",
                cacheKey,
                etag);

            if (requestETag == etag)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.ContentLength = 0;
                context.Response.Body = originalBody;
                return;
            }

            ms.Position = 0;
            context.Response.ContentLength = ms.Length;
            await ms.CopyToAsync(originalBody);
        }
        else
        {
            ms.Position = 0;
            context.Response.ContentLength = ms.Length;
            await ms.CopyToAsync(originalBody);
        }

        context.Response.Body = originalBody;
    }

    private static void ResetCache()
    {
        var previous = Interlocked.Exchange(ref _resetCacheToken, new CancellationTokenSource());
        previous.Cancel();
    }

    private static bool IsCacheable(HttpContext context) =>
        context.Response.StatusCode == StatusCodes.Status200OK;

    private static string ComputeETag(byte[] body)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(body);
        return '"' + Convert.ToBase64String(hash) + '"';
    }

    private record CachedResponse(string ETag, byte[] Body, string ContentType);
}

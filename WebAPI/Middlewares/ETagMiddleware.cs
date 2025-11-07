using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private static readonly ConcurrentDictionary<string, byte> _cacheKeys = new();
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
            await InvalidateRelatedCacheAsync(context.Request.Path);
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
            
            // Track cache key for invalidation
            _cacheKeys.TryAdd(cacheKey, 0);
            
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

    private async Task InvalidateRelatedCacheAsync(string requestPath)
    {
        // Invalidate memory cache using cancellation token
        ResetCache();
        
        // Invalidate distributed cache entries related to this path
        if (_useDistributedCache && _distributedCache != null)
        {
            // Determine which cache keys to invalidate based on the request path
            var keysToRemove = new List<string>();
            
            foreach (var cacheKey in _cacheKeys.Keys)
            {
                // Remove user prefix if present (format: "userId:path")
                var keyWithoutUser = cacheKey.Contains(':') 
                    ? cacheKey.Substring(cacheKey.IndexOf(':') + 1) 
                    : cacheKey;
                
                // Extract the base path (without query string)
                var basePath = keyWithoutUser.Contains('?') 
                    ? keyWithoutUser.Substring(0, keyWithoutUser.IndexOf('?')) 
                    : keyWithoutUser;
                
                // Invalidate if it's the same resource or a related list endpoint
                if (ShouldInvalidateCache(requestPath, basePath))
                {
                    keysToRemove.Add(cacheKey);
                }
            }
            
            // Remove from distributed cache and tracking dictionary
            foreach (var key in keysToRemove)
            {
                try
                {
                    await _distributedCache.RemoveAsync(key);
                    _cacheKeys.TryRemove(key, out _);
                    _logger.LogInformation("Invalidated cache key: {Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate cache key: {Key}", key);
                }
            }
        }
        else if (_memoryCache != null)
        {
            // For memory cache, the cancellation token already invalidates entries
            // But we should also remove tracked keys
            var keysToRemove = new List<string>();
            
            foreach (var cacheKey in _cacheKeys.Keys)
            {
                var keyWithoutUser = cacheKey.Contains(':') 
                    ? cacheKey.Substring(cacheKey.IndexOf(':') + 1) 
                    : cacheKey;
                
                var basePath = keyWithoutUser.Contains('?') 
                    ? keyWithoutUser.Substring(0, keyWithoutUser.IndexOf('?')) 
                    : keyWithoutUser;
                
                if (ShouldInvalidateCache(requestPath, basePath))
                {
                    keysToRemove.Add(cacheKey);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
                _logger.LogInformation("Invalidated memory cache key: {Key}", key);
            }
        }
    }

    private static bool ShouldInvalidateCache(string requestPath, string cachedPath)
    {
        // Exact match
        if (requestPath.Equals(cachedPath, StringComparison.OrdinalIgnoreCase))
            return true;
        
        var requestParts = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var cachedParts = cachedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (requestParts.Length < 2 || cachedParts.Length < 2)
            return false;
        
        // Extract resource base (e.g., "api", "companies" or "api", "licensing")
        var requestBase = $"{requestParts[0]}/{requestParts[1]}";
        var cachedBase = $"{cachedParts[0]}/{cachedParts[1]}";
        
        // Same resource type (e.g., both /api/companies)
        if (requestBase.Equals(cachedBase, StringComparison.OrdinalIgnoreCase))
        {
            // If request is modifying a specific resource (has ID), invalidate list endpoint
            if (requestParts.Length > 2)
            {
                // Request is like /api/companies/{id} - invalidate /api/companies list
                if (cachedParts.Length == 2)
                    return true;
                
                // Also invalidate the specific resource endpoint (same ID)
                if (cachedParts.Length > 2 && requestParts[2].Equals(cachedParts[2], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // If request is modifying list endpoint, invalidate it
            else if (cachedParts.Length == 2)
            {
                return true;
            }
        }
        
        // Cross-resource invalidation: Licensing operations modify company data
        // When /api/licensing/{id}/activate (or suspend, resume, extend, etc.) is called,
        // invalidate company-related cache entries
        if (requestBase.Equals("api/licensing", StringComparison.OrdinalIgnoreCase) &&
            requestParts.Length >= 3)
        {
            // Extract company ID from licensing path (e.g., /api/licensing/{id}/activate)
            var companyIdFromRequest = requestParts[2]; // The ID in /api/licensing/{id}/...
            
            // Invalidate company list endpoint
            if (cachedBase.Equals("api/companies", StringComparison.OrdinalIgnoreCase) &&
                cachedParts.Length == 2)
            {
                return true;
            }
            
            // Invalidate specific company endpoint (same ID)
            if (cachedBase.Equals("api/companies", StringComparison.OrdinalIgnoreCase) &&
                cachedParts.Length > 2 &&
                companyIdFromRequest.Equals(cachedParts[2], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Invalidate licensing status endpoint for the same company
            if (cachedBase.Equals("api/licensing", StringComparison.OrdinalIgnoreCase) &&
                cachedParts.Length >= 3 &&
                companyIdFromRequest.Equals(cachedParts[2], StringComparison.OrdinalIgnoreCase) &&
                cachedParts.Length > 3 &&
                cachedParts[3].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        // Cross-resource invalidation: Company operations should also invalidate licensing status
        if (requestBase.Equals("api/companies", StringComparison.OrdinalIgnoreCase) &&
            requestParts.Length >= 3)
        {
            var companyIdFromRequest = requestParts[2];
            
            // Invalidate licensing status endpoint for the same company
            if (cachedBase.Equals("api/licensing", StringComparison.OrdinalIgnoreCase) &&
                cachedParts.Length >= 3 &&
                companyIdFromRequest.Equals(cachedParts[2], StringComparison.OrdinalIgnoreCase) &&
                cachedParts.Length > 3 &&
                cachedParts[3].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
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

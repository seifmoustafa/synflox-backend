using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.HealthChecks;

/// <summary>
/// Health check for Redis connectivity (optional).
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to set and get a test value
            var testKey = $"health_check_{Guid.NewGuid()}";
            var testValue = "test";
            
            await _cache.SetStringAsync(testKey, testValue, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            }, cancellationToken);

            var retrievedValue = await _cache.GetStringAsync(testKey, cancellationToken);
            
            if (retrievedValue != testValue)
            {
                return HealthCheckResult.Degraded("Redis is accessible but data integrity check failed");
            }

            // Clean up test key
            await _cache.RemoveAsync(testKey, cancellationToken);

            return HealthCheckResult.Healthy("Redis is accessible and working correctly");
        }
        catch (Exception ex)
        {
            // Redis is optional, so return Degraded instead of Unhealthy
            return HealthCheckResult.Degraded("Redis is not accessible (optional service)", ex);
        }
    }
}




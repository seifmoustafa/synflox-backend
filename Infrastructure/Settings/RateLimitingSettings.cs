using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

/// <summary>
/// Settings for API rate limiting.
/// </summary>
public class RateLimitingSettings
{
    /// <summary>
    /// Global rate limit per hour for all requests.
    /// </summary>
    [Range(1, 1000000)]
    public int GlobalLimitPerHour { get; set; } = 1000;

    /// <summary>
    /// Default rate limit per hour for API keys (if not specified per key).
    /// </summary>
    [Range(1, 100000)]
    public int DefaultApiKeyLimitPerHour { get; set; } = 100;

    /// <summary>
    /// Per-endpoint rate limits. Key is the endpoint path pattern, value is requests per hour.
    /// </summary>
    public Dictionary<string, int> EndpointLimits { get; set; } = new Dictionary<string, int>
    {
        { "/api/licensing/{id}/status", 100 },
        { "/api/licensing/validate-key", 50 }
    };

    /// <summary>
    /// Whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}


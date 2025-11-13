using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

/// <summary>
/// Configuration settings for client access tokens
/// </summary>
public class ClientTokenSettings
{
    /// <summary>
    /// JWT signing key for client tokens (different from admin JWT)
    /// </summary>
    [Required]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer for client tokens
    /// </summary>
    [Required]
    public string Issuer { get; set; } = "SYNFLOX-ClientAPI";

    /// <summary>
    /// JWT audience for client tokens
    /// </summary>
    [Required]
    public string Audience { get; set; } = "SYNFLOX-Clients";

    /// <summary>
    /// Default token expiry in days (if not set to subscription expiry)
    /// </summary>
    public int DefaultExpiryDays { get; set; } = 365;

    /// <summary>
    /// Maximum token expiry in days (security limit)
    /// </summary>
    public int MaxExpiryDays { get; set; } = 1095; // 3 years

    /// <summary>
    /// Whether to use subscription expiry as token expiry
    /// </summary>
    public bool UseSubscriptionExpiry { get; set; } = true;

    /// <summary>
    /// Rate limiting settings
    /// </summary>
    public int RateLimitPerHour { get; set; } = 1000;
    public int RateLimitPerDay { get; set; } = 10000;
    public int RateLimitPerMonth { get; set; } = 100000;

    /// <summary>
    /// Token cleanup settings
    /// </summary>
    public int CleanupExpiredTokensAfterDays { get; set; } = 30;
    public int CleanupUsageLogsAfterDays { get; set; } = 90;

    /// <summary>
    /// Security settings
    /// </summary>
    public bool RequireHttps { get; set; } = true;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Allowed client endpoints by default
    /// </summary>
    public List<string> DefaultAllowedEndpoints { get; set; } = new()
    {
        "/api/client/subscription/status",
        "/api/client/license/validate",
        "/api/client/company/profile",
        "/api/client/auth/validate-token",
        "/api/client/health"
    };

    /// <summary>
    /// Token version for future compatibility
    /// </summary>
    public string TokenVersion { get; set; } = "1.0";

    /// <summary>
    /// Whether to auto-generate tokens for new subscriptions
    /// </summary>
    public bool AutoGenerateForNewSubscriptions { get; set; } = true;

    /// <summary>
    /// Whether to revoke tokens when subscription becomes inactive
    /// </summary>
    public bool RevokeOnSubscriptionInactive { get; set; } = true;

    /// <summary>
    /// Notification settings
    /// </summary>
    public bool NotifyOnTokenExpiry { get; set; } = true;
    public int NotifyDaysBeforeExpiry { get; set; } = 7;

    /// <summary>
    /// Analytics settings
    /// </summary>
    public bool EnableUsageAnalytics { get; set; } = true;
    public bool LogDetailedUsage { get; set; } = true;
    public int AnalyticsRetentionDays { get; set; } = 365;
}

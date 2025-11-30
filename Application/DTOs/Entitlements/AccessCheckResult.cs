using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Result DTO for access check operations
/// </summary>
public class AccessCheckResult
{
    /// <summary>
    /// Whether access is granted
    /// </summary>
    public bool HasAccess { get; set; }
    
    /// <summary>
    /// The access level granted (None if no access)
    /// </summary>
    public EntitlementAccessLevel AccessLevel { get; set; }
    
    /// <summary>
    /// Reason for access denial (if HasAccess is false)
    /// </summary>
    public string? DenialReason { get; set; }
    
    /// <summary>
    /// The subscription's current access mode
    /// </summary>
    public SubscriptionAccessMode SubscriptionAccessMode { get; set; }
    
    /// <summary>
    /// Allowed operations for this resource
    /// </summary>
    public List<string> AllowedOperations { get; set; } = new();
    
    /// <summary>
    /// Resource-specific features available
    /// </summary>
    public List<string> AvailableFeatures { get; set; } = new();
    
    /// <summary>
    /// Usage limits for this resource
    /// </summary>
    public Dictionary<string, object> UsageLimits { get; set; } = new();
    
    /// <summary>
    /// When this entitlement expires (null if no expiry)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Custom restriction message to show user
    /// </summary>
    public string? RestrictionMessage { get; set; }
    
    /// <summary>
    /// Upgrade CTA if access is limited
    /// </summary>
    public string? UpgradeCta { get; set; }
    
    /// <summary>
    /// Upgrade URL if access is limited
    /// </summary>
    public string? UpgradeUrl { get; set; }
    
    #region Factory Methods
    
    public static AccessCheckResult Granted(
        EntitlementAccessLevel level = EntitlementAccessLevel.Full,
        List<string>? operations = null,
        List<string>? features = null)
    {
        return new AccessCheckResult
        {
            HasAccess = true,
            AccessLevel = level,
            AllowedOperations = operations ?? new List<string> { "GET", "POST", "PUT", "DELETE", "EXPORT" },
            AvailableFeatures = features ?? new List<string>()
        };
    }
    
    public static AccessCheckResult Denied(
        string reason,
        SubscriptionAccessMode? mode = null,
        string? upgradeUrl = null,
        string? upgradeCta = null)
    {
        return new AccessCheckResult
        {
            HasAccess = false,
            AccessLevel = EntitlementAccessLevel.None,
            DenialReason = reason,
            SubscriptionAccessMode = mode ?? SubscriptionAccessMode.Blocked,
            UpgradeUrl = upgradeUrl,
            UpgradeCta = upgradeCta
        };
    }
    
    public static AccessCheckResult ReadOnly(string? reason = null)
    {
        return new AccessCheckResult
        {
            HasAccess = true,
            AccessLevel = EntitlementAccessLevel.ReadOnly,
            AllowedOperations = new List<string> { "GET" },
            RestrictionMessage = reason ?? "Read-only access. Upgrade to modify data."
        };
    }
    
    public static AccessCheckResult ExportOnly(string? reason = null)
    {
        return new AccessCheckResult
        {
            HasAccess = true,
            AccessLevel = EntitlementAccessLevel.ExportOnly,
            AllowedOperations = new List<string> { "GET", "EXPORT" },
            RestrictionMessage = reason ?? "Export-only access. Data export available until deadline."
        };
    }
    
    #endregion
}

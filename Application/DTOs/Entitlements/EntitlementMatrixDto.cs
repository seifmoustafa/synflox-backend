using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Complete entitlement matrix for a subscription
/// Returned by GET /api/client/entitlements
/// </summary>
public record EntitlementMatrixDto
{
    /// <summary>
    /// Subscription ID
    /// </summary>
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Company ID
    /// </summary>
    public Guid CompanyId { get; set; }
    
    /// <summary>
    /// Version number (increments on any entitlement change)
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Current access mode for the subscription
    /// </summary>
    public SubscriptionAccessMode AccessMode { get; set; }
    
    /// <summary>
    /// Human-readable access mode
    /// </summary>
    public string AccessModeDisplay => AccessMode switch
    {
        SubscriptionAccessMode.Full => "Full Access",
        SubscriptionAccessMode.GracePeriod => "Grace Period",
        SubscriptionAccessMode.ReadOnly => "Read Only",
        SubscriptionAccessMode.ExportOnly => "Export Only",
        SubscriptionAccessMode.Blocked => "Blocked",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Subscription expiry date
    /// </summary>
    public DateTime ExpiryDateUtc { get; set; }
    
    /// <summary>
    /// Export deadline (for ExportOnly mode)
    /// </summary>
    public DateTime? ExportDeadlineUtc { get; set; }
    
    /// <summary>
    /// Custom restriction message
    /// </summary>
    public string? AccessRestrictionMessage { get; set; }
    
    /// <summary>
    /// Entitled projects with their modules
    /// </summary>
    public List<ProjectEntitlementDto> Projects { get; set; } = new();
    
    /// <summary>
    /// Standalone modules (not part of a project)
    /// </summary>
    public List<ModuleEntitlementDto> StandaloneModules { get; set; } = new();
    
    /// <summary>
    /// Global usage limits for the subscription
    /// </summary>
    public Dictionary<string, object> GlobalUsageLimits { get; set; } = new();
    
    /// <summary>
    /// Timestamp when this matrix was generated
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Cache TTL in seconds (client should refresh after this)
    /// </summary>
    public int CacheTtlSeconds { get; set; } = 86400; // 24 hours
    
    #region Marketing/Upgrade Support
    
    /// <summary>
    /// Modules available for upgrade (not currently entitled)
    /// </summary>
    public List<AvailableUpgradeDto> AvailableUpgrades { get; set; } = new();
    
    /// <summary>
    /// Menu display configuration
    /// </summary>
    public MenuConfigDto MenuConfig { get; set; } = new();
    
    #endregion
}

using System;
using System.Collections.Generic;
using Domain.Enums;
using AccessMode = Domain.Enums.SubscriptionAccessMode;
using AccessLevel = Domain.Enums.EntitlementAccessLevel;

namespace Domain.ValueObjects;

/// <summary>
/// Represents the complete entitlement matrix for a subscription.
/// Used by both Online (fetched via API) and Offline (embedded in license key) systems.
/// </summary>
public class EntitlementMatrix
{
    /// <summary>
    /// Version number for cache invalidation.
    /// Incremented when entitlements change.
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Current access mode (Full, ReadOnly, GracePeriod, Blocked).
    /// </summary>
    public SubscriptionAccessMode AccessMode { get; set; } = SubscriptionAccessMode.Full;
    
    /// <summary>
    /// Days remaining until subscription expires.
    /// </summary>
    public int DaysRemaining { get; set; }
    
    /// <summary>
    /// Whether the subscription is in grace period.
    /// </summary>
    public bool IsInGracePeriod { get; set; }
    
    /// <summary>
    /// When the entitlements were last updated.
    /// </summary>
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Project-level access rights.
    /// </summary>
    public List<ProjectAccess> Projects { get; set; } = new();
    
    /// <summary>
    /// Module-level access rights.
    /// </summary>
    public List<ModuleAccess> Modules { get; set; } = new();
}

/// <summary>
/// Access rights for a specific project.
/// </summary>
public class ProjectAccess
{
    public Guid ProjectId { get; set; }
    public required string ProjectName { get; set; }
    public required string ProjectCode { get; set; }
    public EntitlementAccessLevel AccessLevel { get; set; }
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
}

/// <summary>
/// Access rights for a specific module.
/// </summary>
public class ModuleAccess
{
    public Guid ModuleId { get; set; }
    public Guid? ProjectId { get; set; }
    public required string ModuleName { get; set; }
    public required string ModuleCode { get; set; }
    public EntitlementAccessLevel AccessLevel { get; set; }
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public List<string> Features { get; set; } = new();
}

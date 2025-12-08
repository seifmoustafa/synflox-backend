using System;
using System.Collections.Generic;
using Domain.Enums;
using AccessMode = Domain.Enums.SubscriptionAccessMode;
using AccessLevel = Domain.Enums.EntitlementAccessLevel;

namespace Application.DTOs.OnlineAccess;

/// <summary>
/// DTO for the entitlement matrix returned to online clients.
/// </summary>
public class EntitlementMatrixDto
{
    /// <summary>
    /// Version number for cache invalidation.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Current access mode.
    /// </summary>
    public SubscriptionAccessMode AccessMode { get; set; }
    
    /// <summary>
    /// Human-readable access mode.
    /// </summary>
    public string AccessModeDisplay => AccessMode.ToString();
    
    /// <summary>
    /// Days remaining until subscription expires.
    /// </summary>
    public int DaysRemaining { get; set; }
    
    /// <summary>
    /// Whether the subscription is in grace period.
    /// </summary>
    public bool IsInGracePeriod { get; set; }
    
    /// <summary>
    /// Subscription expiry date.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
    
    /// <summary>
    /// When the entitlements were last updated.
    /// </summary>
    public DateTime LastUpdatedUtc { get; set; }
    
    /// <summary>
    /// Project-level access rights.
    /// </summary>
    public List<ProjectAccessDto> Projects { get; set; } = new();
    
    /// <summary>
    /// Module-level access rights.
    /// </summary>
    public List<ModuleAccessDto> Modules { get; set; } = new();
}

/// <summary>
/// DTO for project access rights.
/// </summary>
public class ProjectAccessDto
{
    public Guid ProjectId { get; set; }
    public required string ProjectName { get; set; }
    public required string ProjectCode { get; set; }
    public EntitlementAccessLevel AccessLevel { get; set; }
    public string AccessLevelDisplay => AccessLevel.ToString();
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
}

/// <summary>
/// DTO for module access rights.
/// </summary>
public class ModuleAccessDto
{
    public Guid ModuleId { get; set; }
    public Guid? ProjectId { get; set; }
    public required string ModuleName { get; set; }
    public required string ModuleCode { get; set; }
    public EntitlementAccessLevel AccessLevel { get; set; }
    public string AccessLevelDisplay => AccessLevel.ToString();
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// DTO for subscription status (for online clients).
/// </summary>
public class OnlineSubscriptionStatusDto
{
    public Guid SubscriptionId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public required string PlanName { get; set; }
    public required string CompanyName { get; set; }
    public required string Status { get; set; }
    public SubscriptionAccessMode AccessMode { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsActive { get; set; }
    public bool IsTrial { get; set; }
    public bool IsInGracePeriod { get; set; }
    public int EntitlementVersion { get; set; }
    public int? MaxDevices { get; set; }
    public int CurrentDeviceCount { get; set; }
    public bool HasPendingChanges { get; set; }
    public int PendingChangeCount { get; set; }
}

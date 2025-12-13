using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.PlanDto;

public class PlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// The type of duration for this plan (Weekly, Monthly, Yearly, Lifetime, etc.)
    /// This is the SINGLE SOURCE OF TRUTH - expiry dates calculated from this.
    /// </summary>
    public PlanDurationType DurationType { get; set; }
    
    /// <summary>
    /// Indicates if this is a lifetime/permanent plan
    /// </summary>
    public bool IsLifetimePlan { get; set; }
    
    /// <summary>
    /// Human-readable duration description (e.g., "Monthly (1 month)", "Lifetime (Never Expires)")
    /// </summary>
    public string DurationDescription { get; set; } = string.Empty;
    
    public bool AllowTrial { get; set; }
    public int? TrialDurationDays { get; set; }
    public bool AutoRenew { get; set; }
    public UpgradePolicy UpgradePolicy { get; set; }
    public int GracePeriodDays { get; set; }
    public List<string> CustomFeatures { get; set; } = new();
    public List<PlanPriceDto> Prices { get; set; } = new();
    
    /// <summary>
    /// Number of projects directly included in this plan
    /// </summary>
    public int ProjectCount { get; set; }
    
    /// <summary>
    /// Number of modules directly included in this plan
    /// </summary>
    public int ModuleCount { get; set; }
    
    #region Free Tier & Fallback (Enterprise Entitlement System)
    
    /// <summary>
    /// Whether this is a free tier plan
    /// </summary>
    public bool IsFreeTier { get; set; }
    
    /// <summary>
    /// Access mode for fallback when subscription expires
    /// </summary>
    public SubscriptionAccessMode FallbackAccessMode { get; set; }
    
    /// <summary>
    /// Days allowed for data export after access is blocked
    /// </summary>
    public int ExportGraceDays { get; set; }
    
    /// <summary>
    /// Default fallback plan ID
    /// </summary>
    public Guid? DefaultFallbackPlanId { get; set; }
    
    /// <summary>
    /// Default fallback plan name
    /// </summary>
    public string? DefaultFallbackPlanName { get; set; }
    
    /// <summary>
    /// Show locked modules in menu for marketing
    /// </summary>
    public bool ShowLockedModulesInMenu { get; set; }
    
    /// <summary>
    /// Style for locked menu items
    /// </summary>
    public string LockedItemStyle { get; set; } = "greyed_with_lock";
    
    #endregion
    
    #region Plan Hierarchy (Inheritance)
    
    /// <summary>
    /// Parent plan ID for feature inheritance
    /// </summary>
    public Guid? ParentPlanId { get; set; }
    
    /// <summary>
    /// Parent plan name for display
    /// </summary>
    public string? ParentPlanName { get; set; }
    
    /// <summary>
    /// Display order for plan hierarchy (lower = shown first)
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Number of child plans that inherit from this plan
    /// </summary>
    public int ChildPlanCount { get; set; }
    
    /// <summary>
    /// Total inherited projects count (from parent chain)
    /// </summary>
    public int InheritedProjectsCount { get; set; }
    
    /// <summary>
    /// Total inherited modules count (from parent chain)
    /// </summary>
    public int InheritedModulesCount { get; set; }
    
    #endregion
    
    #region Device Activation Limits
    
    /// <summary>
    /// Maximum number of devices that can be bound to this plan.
    /// 0 = unlimited devices (no binding required)
    /// </summary>
    public int MaxDevices { get; set; }
    
    /// <summary>
    /// Whether machine binding is required for license validation.
    /// </summary>
    public bool RequireMachineBinding { get; set; }
    
    /// <summary>
    /// Policy for handling device replacement when max devices reached.
    /// </summary>
    public DeviceReplacementPolicy DeviceReplacementPolicy { get; set; }
    
    /// <summary>
    /// Number of hardware component changes allowed before requiring re-binding.
    /// </summary>
    public int HardwareChangeTolerance { get; set; }
    
    /// <summary>
    /// How concurrent device access is handled.
    /// </summary>
    public ConcurrentAccessMode ConcurrentAccessMode { get; set; }
    
    /// <summary>
    /// Maximum concurrent devices allowed (for LimitedConcurrent/TimeBasedLimited modes).
    /// </summary>
    public int MaxConcurrentDevices { get; set; }
    
    /// <summary>
    /// Timeout in minutes for device heartbeat detection.
    /// </summary>
    public int DeviceHeartbeatTimeoutMinutes { get; set; }
    
    /// <summary>
    /// Controls how devices are admitted/registered for subscriptions on this plan.
    /// Open = Any device can register freely up to limit
    /// AdminOnly = Company admin must explicitly bind each device
    /// AutoWithQueue = Auto-register up to limit, then queue for admin approval
    /// HybridAutoAdmin = First N auto-register, rest require admin
    /// </summary>
    public DeviceAdmissionMode DeviceAdmissionMode { get; set; }
    
    /// <summary>
    /// For HybridAutoAdmin mode: Number of devices that auto-register before requiring admin approval.
    /// Only used when DeviceAdmissionMode = HybridAutoAdmin.
    /// </summary>
    public int MaxAutoAdmitDevices { get; set; }
    
    #endregion
}

public class PlanPriceDto
{
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
}

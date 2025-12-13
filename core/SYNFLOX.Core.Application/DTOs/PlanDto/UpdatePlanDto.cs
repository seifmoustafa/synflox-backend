using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.PlanDto;

public class UpdatePlanDto
{
    [StringLength(150, MinimumLength = 2)]
    public string? Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// The type of duration for this plan (Weekly, Monthly, Yearly, Lifetime, etc.)
    /// This is the SINGLE SOURCE OF TRUTH - expiry dates are calculated directly from this.
    /// </summary>
    public PlanDurationType? DurationType { get; set; }

    public List<PlanPriceDto>? Prices { get; set; }

    /// <summary>
    /// Allow trial period for this plan
    /// ⚠️ LIFETIME PLANS: This MUST be false or null (request will be rejected if true)
    /// </summary>
    public bool? AllowTrial { get; set; }

    [Range(1, 60)]
    public int? TrialDurationDays { get; set; }

    /// <summary>
    /// Enable auto-renewal for this plan
    /// ⚠️ LIFETIME PLANS: This MUST be false or null (request will be rejected if true)
    /// </summary>
    public bool? AutoRenew { get; set; }

    public UpgradePolicy? UpgradePolicy { get; set; }

    [Range(0, 30)]
    public int? GracePeriodDays { get; set; }

    [MaxLength(200)]
    public List<string>? CustomFeatures { get; set; }

    public List<Guid>? ProjectIds { get; set; }

    public List<Guid>? ModuleIds { get; set; }
    
    #region Enterprise Entitlement System
    
    /// <summary>
    /// Mark this as a free tier plan (limited access)
    /// </summary>
    public bool? IsFreeTier { get; set; }
    
    /// <summary>
    /// Access mode when subscription falls back
    /// </summary>
    public SubscriptionAccessMode? FallbackAccessMode { get; set; }
    
    /// <summary>
    /// Days allowed for data export after access is blocked (0-90)
    /// </summary>
    [Range(0, 90)]
    public int? ExportGraceDays { get; set; }
    
    /// <summary>
    /// Default fallback plan ID (optional)
    /// </summary>
    public Guid? DefaultFallbackPlanId { get; set; }
    
    /// <summary>
    /// Show locked modules in menu (with lock icon)
    /// </summary>
    public bool? ShowLockedModulesInMenu { get; set; }
    
    /// <summary>
    /// Style for locked items (greyed_with_lock, hidden, etc.)
    /// </summary>
    [StringLength(50)]
    public string? LockedItemStyle { get; set; }
    
    #endregion
    
    #region Plan Hierarchy
    
    /// <summary>
    /// Parent plan ID for feature inheritance
    /// </summary>
    public Guid? ParentPlanId { get; set; }
    
    /// <summary>
    /// Display order for plan hierarchy
    /// </summary>
    public int? DisplayOrder { get; set; }
    
    #endregion
    
    #region Device Activation Limits
    
    /// <summary>
    /// Maximum number of devices that can be bound to this plan.
    /// 0 = unlimited devices (no binding required)
    /// </summary>
    [Range(0, 1000)]
    public int? MaxDevices { get; set; }
    
    /// <summary>
    /// Require machine binding for license validation.
    /// </summary>
    public bool? RequireMachineBinding { get; set; }
    
    /// <summary>
    /// Policy for handling device replacement when max devices reached.
    /// </summary>
    public DeviceReplacementPolicy? DeviceReplacementPolicy { get; set; }
    
    /// <summary>
    /// Number of hardware component changes allowed before requiring re-binding.
    /// </summary>
    [Range(0, 10)]
    public int? HardwareChangeTolerance { get; set; }
    
    /// <summary>
    /// How concurrent device access is handled.
    /// </summary>
    public ConcurrentAccessMode? ConcurrentAccessMode { get; set; }
    
    /// <summary>
    /// Maximum concurrent devices allowed.
    /// </summary>
    public int? MaxConcurrentDevices { get; set; }
    
    /// <summary>
    /// Timeout in minutes for device heartbeat detection.
    /// </summary>
    [Range(5, 1440)]
    public int? DeviceHeartbeatTimeoutMinutes { get; set; }
    
    /// <summary>
    /// Controls how devices are admitted/registered for subscriptions on this plan.
    /// </summary>
    public DeviceAdmissionMode? DeviceAdmissionMode { get; set; }
    
    /// <summary>
    /// For HybridAutoAdmin mode: Number of devices that auto-register before requiring admin approval.
    /// </summary>
    [Range(0, 100)]
    public int? MaxAutoAdmitDevices { get; set; }
    
    #endregion
}

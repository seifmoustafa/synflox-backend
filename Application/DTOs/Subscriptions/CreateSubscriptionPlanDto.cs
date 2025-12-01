using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Application.DTOs.PlanDto;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

public class CreateSubscriptionPlanDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public required string Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// The type of duration for this plan (Weekly, Monthly, Yearly, Lifetime, etc.)
    /// This is the SINGLE SOURCE OF TRUTH - expiry dates are calculated directly from this.
    /// </summary>
    [Required]
    public PlanDurationType DurationType { get; set; } = PlanDurationType.Monthly;

    /// <summary>
    /// Prices in multiple currencies
    /// Required for paid plans (validated in service based on IsFreeTier)
    /// Free tier plans don't need prices - backend auto-sets Currency.Free with Amount=0
    /// </summary>
    public List<PlanPriceDto> Prices { get; set; } = new();

    /// <summary>
    /// Allow trial period for this plan
    /// ⚠️ LIFETIME PLANS: This MUST be false (request will be rejected if true)
    /// </summary>
    public bool AllowTrial { get; set; }

    [Range(1, 60)]
    public int? TrialDurationDays { get; set; }

    /// <summary>
    /// Enable auto-renewal for this plan
    /// ⚠️ LIFETIME PLANS: This MUST be false (request will be rejected if true)
    /// </summary>
    public bool AutoRenew { get; set; }

    [Required]
    public UpgradePolicy UpgradePolicy { get; set; }

    [Range(0, 30)]
    public int GracePeriodDays { get; set; }

    [MaxLength(200)]
    public List<string> CustomFeatures { get; set; } = new();

    /// <summary>
    /// Project IDs to include in this plan
    /// </summary>
    public List<Guid> ProjectIds { get; set; } = new();

    /// <summary>
    /// Module IDs to include in this plan (standalone modules)
    /// </summary>
    public List<Guid> ModuleIds { get; set; } = new();
    
    #region Enterprise Entitlement System
    
    /// <summary>
    /// Mark this as a free tier plan (limited access)
    /// </summary>
    public bool IsFreeTier { get; set; }
    
    /// <summary>
    /// Access mode when subscription falls back (default: ReadOnly)
    /// </summary>
    public SubscriptionAccessMode FallbackAccessMode { get; set; } = SubscriptionAccessMode.ReadOnly;
    
    /// <summary>
    /// Days allowed for data export after access is blocked (0-90, default: 30)
    /// </summary>
    [Range(0, 90)]
    public int ExportGraceDays { get; set; } = 30;
    
    /// <summary>
    /// Default fallback plan ID (optional)
    /// </summary>
    public Guid? DefaultFallbackPlanId { get; set; }
    
    /// <summary>
    /// Show locked modules in menu (with lock icon)
    /// </summary>
    public bool ShowLockedModulesInMenu { get; set; } = true;
    
    /// <summary>
    /// Style for locked items (greyed_with_lock, hidden, etc.)
    /// </summary>
    [StringLength(50)]
    public string LockedItemStyle { get; set; } = "greyed_with_lock";
    
    #endregion
    
    #region Plan Hierarchy
    
    /// <summary>
    /// Parent plan ID for feature inheritance.
    /// Child plans automatically inherit all projects and modules from parent.
    /// Example: Pro (child) inherits from Free (parent).
    /// </summary>
    public Guid? ParentPlanId { get; set; }
    
    /// <summary>
    /// Display order for plan hierarchy (lower = shown first)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
    
    #endregion
}

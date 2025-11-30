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
}

public class PlanPriceDto
{
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
}

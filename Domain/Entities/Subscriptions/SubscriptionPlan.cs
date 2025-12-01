using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Represents a commercial subscription plan (e.g., Basic, Pro, Enterprise)
/// </summary>
public class SubscriptionPlan : AuditEntity<Guid>
{
    [Required]
    [StringLength(150)]
    public required string Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// The type of duration for this plan (Weekly, Monthly, Yearly, Lifetime, etc.)
    /// This is the SINGLE SOURCE OF TRUTH for plan duration.
    /// Expiry dates are calculated directly from this field.
    /// </summary>
    public PlanDurationType DurationType { get; set; } = PlanDurationType.Monthly;

    /// <summary>
    /// Indicates if this is a lifetime/permanent plan
    /// </summary>
    public bool IsLifetimePlan => DurationType == PlanDurationType.Lifetime;

    /// <summary>
    /// Whether this plan allows a trial period
    /// </summary>
    public bool AllowTrial { get; set; }

    /// <summary>
    /// Trial duration in days (required if AllowTrial = true)
    /// </summary>
    [Range(1, 60)]
    public int? TrialDurationDays { get; set; }

    /// <summary>
    /// Whether subscriptions auto-renew by default
    /// Note: Lifetime plans cannot auto-renew (forced to false)
    /// </summary>
    public bool AutoRenew { get; set; }

    /// <summary>
    /// How upgrades from this plan are handled
    /// Note: Lifetime plans can only use FullReplace (cannot prorate or defer)
    /// </summary>
    public UpgradePolicy UpgradePolicy { get; set; }

    /// <summary>
    /// Grace period in days after expiry before full deactivation
    /// Note: Lifetime plans should have 0 grace period (they never expire)
    /// </summary>
    [Range(0, 30)]
    public int GracePeriodDays { get; set; }

    /// <summary>
    /// Custom commercial features/perks (e.g., "24/7 Support", "SLA 1h", "Priority Queue")
    /// </summary>
    public List<string> CustomFeatures { get; set; } = new();

    #region Free Tier & Fallback Configuration (Enterprise Entitlement System)

    /// <summary>
    /// Is this a free tier plan? (Can be used as fallback for expired subscriptions)
    /// Free tier plans typically provide read-only access
    /// </summary>
    public bool IsFreeTier { get; set; }

    /// <summary>
    /// Access mode when using this plan as fallback after expiry
    /// Typically ReadOnly for view + export only access
    /// </summary>
    public SubscriptionAccessMode FallbackAccessMode { get; set; } = SubscriptionAccessMode.ReadOnly;

    /// <summary>
    /// Days to allow data export after subscription becomes blocked
    /// Only applies when no fallback plan is set
    /// After this period, access is completely blocked
    /// </summary>
    [Range(0, 90)]
    public int ExportGraceDays { get; set; } = 30;

    /// <summary>
    /// Default fallback plan for subscribers of this plan
    /// When their subscription expires, they automatically get this fallback plan
    /// null = no automatic fallback (will be blocked or export-only)
    /// </summary>
    public Guid? DefaultFallbackPlanId { get; set; }

    /// <summary>
    /// Whether to show locked modules in menu for marketing purposes
    /// When true, client applications should display modules not in the plan as locked
    /// </summary>
    public bool ShowLockedModulesInMenu { get; set; } = true;

    /// <summary>
    /// Style for displaying locked items in client UI
    /// Options: "greyed_with_lock", "hidden", "upgrade_badge", "separate_section"
    /// </summary>
    [StringLength(50)]
    public string LockedItemStyle { get; set; } = "greyed_with_lock";

    #endregion

    #region Plan Hierarchy (Inheritance)

    /// <summary>
    /// Parent plan for feature inheritance.
    /// Child plans automatically inherit all projects, modules, and custom features from parent.
    /// Example: Pro (child) inherits from Free (parent), Ultra inherits from Pro.
    /// </summary>
    public Guid? ParentPlanId { get; set; }

    /// <summary>
    /// Display order for plan hierarchy (lower = shown first)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    #endregion

    // Navigation properties
    public ICollection<PlanPrice> PlanPrices { get; set; } = new List<PlanPrice>();
    public ICollection<PlanProject> PlanProjects { get; set; } = new List<PlanProject>();
    public ICollection<PlanModule> PlanModules { get; set; } = new List<PlanModule>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    
    /// <summary>
    /// The default fallback plan for this plan's subscribers
    /// </summary>
    public SubscriptionPlan? DefaultFallbackPlan { get; set; }
    
    /// <summary>
    /// Parent plan for inheritance
    /// </summary>
    public SubscriptionPlan? ParentPlan { get; set; }
    
    /// <summary>
    /// Child plans that inherit from this plan
    /// </summary>
    public ICollection<SubscriptionPlan> ChildPlans { get; set; } = new List<SubscriptionPlan>();
}

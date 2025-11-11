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
    /// Duration in months for paid subscriptions (must be > 0)
    /// </summary>
    [Range(1, 120)]
    public int DurationMonths { get; set; }

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
    /// </summary>
    public bool AutoRenew { get; set; }

    /// <summary>
    /// How upgrades from this plan are handled
    /// </summary>
    public UpgradePolicy UpgradePolicy { get; set; }

    /// <summary>
    /// Grace period in days after expiry before full deactivation
    /// </summary>
    [Range(0, 30)]
    public int GracePeriodDays { get; set; }

    /// <summary>
    /// Custom commercial features/perks (e.g., "24/7 Support", "SLA 1h", "Priority Queue")
    /// </summary>
    public List<string> CustomFeatures { get; set; } = new();

    // Navigation properties
    public ICollection<PlanPrice> PlanPrices { get; set; } = new List<PlanPrice>();
    public ICollection<PlanProject> PlanProjects { get; set; } = new List<PlanProject>();
    public ICollection<PlanModule> PlanModules { get; set; } = new List<PlanModule>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

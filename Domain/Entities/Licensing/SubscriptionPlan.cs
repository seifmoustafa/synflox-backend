using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a subscription plan/tier.
/// </summary>
public class SubscriptionPlan : AuditEntity<Guid>
{
    /// <summary>
    /// Name of the subscription plan.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Description of the plan.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Price of the plan.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR, SAR).
    /// </summary>
    [Required]
    [StringLength(3)]
    public required string Currency { get; set; } = "USD";

    /// <summary>
    /// Billing cycle for the plan.
    /// </summary>
    [Required]
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    /// <summary>
    /// Whether the plan is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON array of feature names included in this plan.
    /// This is kept for backward compatibility, but PlanProjectModules is the preferred way.
    /// </summary>
    [StringLength(2000)]
    public string? Features { get; set; } // JSON array of strings

    /// <summary>
    /// Maximum number of companies allowed for this plan (null = unlimited).
    /// </summary>
    public int? MaxCompanies { get; set; }

    /// <summary>
    /// Navigation property to the project-modules included in this plan.
    /// </summary>
    public ICollection<PlanProjectModule> PlanProjectModules { get; set; } = new List<PlanProjectModule>();
}


using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

public class CreateSubscriptionDto
{
    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    /// <summary>
    /// Whether to use custom pricing instead of inheriting from Plan.
    /// If true, Currency and Amount must be provided.
    /// If false, Currency and Amount are inherited from the Plan.
    /// </summary>
    public bool OverridePlanPricing { get; set; }

    /// <summary>
    /// Custom currency for this subscription.
    /// Required when OverridePlanPricing is true.
    /// </summary>
    public Currency? Currency { get; set; }

    /// <summary>
    /// Custom amount for this subscription.
    /// Required when OverridePlanPricing is true.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Whether to start with trial (if plan allows it)
    /// </summary>
    public bool StartWithTrial { get; set; }

    /// <summary>
    /// Override default AutoRenew from plan
    /// </summary>
    public bool? AutoRenew { get; set; }

    /// <summary>
    /// Whether this subscription is for offline use only (vs online API access).
    /// If true: Only offline licensing (license keys, local device activation) is available.
    /// If false: Only online access (API tokens, online device registration) is available.
    /// Default is false (online mode).
    /// </summary>
    public bool IsOffline { get; set; }
}

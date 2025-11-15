using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

public class UpdateSubscriptionPlanDto
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
}

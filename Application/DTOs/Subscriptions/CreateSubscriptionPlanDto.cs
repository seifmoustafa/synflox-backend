using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

public class CreateSubscriptionPlanDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public required string Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [Range(1, 120)]
    public int DurationMonths { get; set; }

    /// <summary>
    /// Prices in multiple currencies
    /// At least one price is required
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<PlanPriceDto> Prices { get; set; } = new();

    public bool AllowTrial { get; set; }

    [Range(1, 60)]
    public int? TrialDurationDays { get; set; }

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
}

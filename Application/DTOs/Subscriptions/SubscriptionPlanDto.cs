using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMonths { get; set; }
    public bool AllowTrial { get; set; }
    public int? TrialDurationDays { get; set; }
    public bool AutoRenew { get; set; }
    public UpgradePolicy UpgradePolicy { get; set; }
    public int GracePeriodDays { get; set; }
    public List<string> CustomFeatures { get; set; } = new();
    public List<PlanPriceDto> Prices { get; set; } = new();
}

public class PlanPriceDto
{
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
}

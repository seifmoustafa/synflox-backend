using System;
using Domain.Enums;

namespace Application.DTOs.SubscriptionPlan;

/// <summary>
/// DTO for subscription plan information.
/// </summary>
public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public BillingCycle BillingCycle { get; set; }
    public bool IsActive { get; set; }
    public string[]? Features { get; set; }
    public int? MaxCompanies { get; set; }
    public PlanTier PlanTier { get; set; }
    public Guid? ParentPlanId { get; set; }
    public string? ParentPlanName { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}


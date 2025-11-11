using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.SubscriptionPlan;

/// <summary>
/// Request DTO for setting a subscription plan's parent.
/// </summary>
public class SetPlanParentRequest
{
    /// <summary>
    /// The encrypted ID of the parent plan. Null to remove parent.
    /// </summary>
    public string? ParentPlanId { get; set; }
}

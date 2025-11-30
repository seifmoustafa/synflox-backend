using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to copy plan entitlements to a subscription
/// </summary>
public class CopyPlanEntitlementsRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    [Required]
    public Guid PlanId { get; set; }
}

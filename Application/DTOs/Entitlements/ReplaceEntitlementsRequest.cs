using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to replace all entitlements with new plan's entitlements
/// </summary>
public class ReplaceEntitlementsRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    [Required]
    public Guid NewPlanId { get; set; }
    
    /// <summary>
    /// Whether to keep custom entitlement grants when replacing
    /// </summary>
    public bool KeepCustomGrants { get; set; } = false;
}

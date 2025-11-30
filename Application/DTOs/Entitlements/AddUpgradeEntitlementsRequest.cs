using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to add upgrade entitlements to a subscription
/// </summary>
public class AddUpgradeEntitlementsRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    [Required]
    public Guid NewPlanId { get; set; }
    
    /// <summary>
    /// Whether to keep custom entitlement grants when upgrading
    /// </summary>
    public bool KeepCustomGrants { get; set; } = true;
}

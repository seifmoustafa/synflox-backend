using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to get entitlement matrix for a subscription
/// </summary>
public class GetEntitlementMatrixRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
}

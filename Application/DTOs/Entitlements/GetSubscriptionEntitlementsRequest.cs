using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to get entitlements for a subscription
/// </summary>
public class GetSubscriptionEntitlementsRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to get entitlements version for a subscription
/// </summary>
public class GetEntitlementsVersionRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
}

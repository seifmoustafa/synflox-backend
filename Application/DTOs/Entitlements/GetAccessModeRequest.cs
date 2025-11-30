using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to get access mode for a subscription
/// </summary>
public class GetAccessModeRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
}

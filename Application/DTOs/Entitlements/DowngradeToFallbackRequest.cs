using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to downgrade subscription to fallback plan
/// </summary>
public class DowngradeToFallbackRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Optional specific fallback plan ID. If null, uses subscription's configured fallback.
    /// </summary>
    public Guid? FallbackPlanId { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to update access mode for a subscription
/// </summary>
public class UpdateAccessModeRequest
{
    public Guid SubscriptionId { get; set; }
    
    [Required]
    public SubscriptionAccessMode NewAccessMode { get; set; }
    
    /// <summary>
    /// Optional message explaining the restriction
    /// </summary>
    public string? RestrictionMessage { get; set; }
}

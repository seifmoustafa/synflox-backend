using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to increment entitlements version
/// </summary>
public class IncrementVersionRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
}

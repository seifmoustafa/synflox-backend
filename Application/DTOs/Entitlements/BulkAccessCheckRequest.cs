using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request for checking multiple resources at once
/// </summary>
public class BulkAccessCheckRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// List of resource checks to perform
    /// </summary>
    [Required]
    public AccessCheckItem[] Checks { get; set; } = Array.Empty<AccessCheckItem>();
}

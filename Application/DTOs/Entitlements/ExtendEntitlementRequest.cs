using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request for extending entitlement expiry date
/// </summary>
public class ExtendEntitlementRequest
{
    [Required]
    public Guid EntitlementId { get; set; }
    
    /// <summary>
    /// New expiry date (must be after current expiry)
    /// </summary>
    [Required]
    public DateTime NewExpiryDate { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

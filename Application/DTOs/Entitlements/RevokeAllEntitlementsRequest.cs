using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request for revoking all entitlements for a subscription
/// </summary>
public class RevokeAllEntitlementsRequest
{
    /// <summary>
    /// Subscription ID (encrypted GUID)
    /// </summary>
    [Required]
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Reason for revocation
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to soft delete (default) or hard delete
    /// </summary>
    public bool HardDelete { get; set; } = false;
}

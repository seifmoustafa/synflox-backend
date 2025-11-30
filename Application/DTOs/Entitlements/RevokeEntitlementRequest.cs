using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request DTO for revoking an entitlement
/// </summary>
public class RevokeEntitlementRequest
{
    /// <summary>
    /// Entitlement ID to revoke (encrypted GUID)
    /// </summary>
    [Required]
    public Guid EntitlementId { get; set; }
    
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

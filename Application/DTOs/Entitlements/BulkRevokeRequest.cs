using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request for bulk revocation of entitlements
/// </summary>
public class BulkRevokeRequest
{
    /// <summary>
    /// List of entitlement IDs to revoke
    /// </summary>
    [Required]
    public List<Guid> EntitlementIds { get; set; } = new();
    
    /// <summary>
    /// Reason for revocation
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

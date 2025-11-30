using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request DTO for updating an existing entitlement
/// </summary>
public class UpdateEntitlementRequest
{
    /// <summary>
    /// Entitlement ID to update (encrypted GUID)
    /// </summary>
    [Required]
    public Guid EntitlementId { get; set; }
    
    /// <summary>
    /// New access level (null = no change)
    /// </summary>
    public EntitlementAccessLevel? AccessLevel { get; set; }
    
    /// <summary>
    /// Custom CRUD permissions (null = derive from AccessLevel)
    /// </summary>
    public bool? CanCreate { get; set; }
    public bool? CanRead { get; set; }
    public bool? CanUpdate { get; set; }
    public bool? CanDelete { get; set; }
    public bool? CanExport { get; set; }
    
    /// <summary>
    /// Updated features list (null = no change)
    /// </summary>
    public List<string>? Features { get; set; }
    
    /// <summary>
    /// Updated usage limits (null = no change)
    /// </summary>
    public Dictionary<string, object>? UsageLimits { get; set; }
    
    /// <summary>
    /// Updated icon (null = no change)
    /// </summary>
    [StringLength(100)]
    public string? Icon { get; set; }
    
    /// <summary>
    /// Updated display in menu flag (null = no change)
    /// </summary>
    public bool? DisplayInMenu { get; set; }
    
    /// <summary>
    /// Updated upgrade CTA (null = no change)
    /// </summary>
    [StringLength(200)]
    public string? UpgradeCta { get; set; }
    
    /// <summary>
    /// Updated upgrade URL (null = no change)
    /// </summary>
    [StringLength(500)]
    public string? UpgradeUrl { get; set; }
    
    /// <summary>
    /// Updated expiry date (null = no change, DateTime.MinValue = remove expiry)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Updated notes
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Active status (null = no change)
    /// </summary>
    public bool? IsActive { get; set; }
}

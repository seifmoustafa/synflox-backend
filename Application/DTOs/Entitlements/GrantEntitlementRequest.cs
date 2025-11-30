using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request DTO for granting a new entitlement to a subscription
/// </summary>
public class GrantEntitlementRequest
{
    /// <summary>
    /// Target subscription (encrypted GUID)
    /// </summary>
    [Required]
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Project to grant access to (null for standalone module)
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    /// <summary>
    /// Module to grant access to (null for full project access)
    /// </summary>
    public Guid? ModuleId { get; set; }
    
    /// <summary>
    /// How this entitlement is being granted
    /// </summary>
    [Required]
    public EntitlementGrantType GrantType { get; set; }
    
    /// <summary>
    /// Access level for this entitlement
    /// </summary>
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;
    
    /// <summary>
    /// Source of the grant
    /// </summary>
    public EntitlementSource Source { get; set; } = EntitlementSource.AdminGrant;
    
    /// <summary>
    /// Custom CRUD permissions (overrides AccessLevel if provided)
    /// </summary>
    public bool? CanCreate { get; set; }
    public bool? CanRead { get; set; }
    public bool? CanUpdate { get; set; }
    public bool? CanDelete { get; set; }
    public bool? CanExport { get; set; }
    
    /// <summary>
    /// Specific features to enable (JSON array, null = all)
    /// </summary>
    public List<string>? Features { get; set; }
    
    /// <summary>
    /// Usage limits (JSON object)
    /// </summary>
    public Dictionary<string, object>? UsageLimits { get; set; }
    
    /// <summary>
    /// Icon for UI display
    /// </summary>
    [StringLength(100)]
    public string? Icon { get; set; }
    
    /// <summary>
    /// Show in menu
    /// </summary>
    public bool DisplayInMenu { get; set; } = true;
    
    /// <summary>
    /// Custom upgrade call-to-action
    /// </summary>
    [StringLength(200)]
    public string? UpgradeCta { get; set; }
    
    /// <summary>
    /// Custom upgrade URL
    /// </summary>
    [StringLength(500)]
    public string? UpgradeUrl { get; set; }
    
    /// <summary>
    /// Optional expiry date for this entitlement
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Admin notes
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}

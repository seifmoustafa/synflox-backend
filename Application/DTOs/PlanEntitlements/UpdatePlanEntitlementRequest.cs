using System;
using Domain.Enums;

namespace Application.DTOs.PlanEntitlements;

/// <summary>
/// Request DTO for updating an existing plan entitlement
/// </summary>
public class UpdatePlanEntitlementRequest
{
    /// <summary>
    /// Entitlement ID (encrypted)
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Access level for this entitlement
    /// </summary>
    public EntitlementAccessLevel? AccessLevel { get; set; }
    
    /// <summary>
    /// Can create new records
    /// </summary>
    public bool? CanCreate { get; set; }
    
    /// <summary>
    /// Can read/view records
    /// </summary>
    public bool? CanRead { get; set; }
    
    /// <summary>
    /// Can update existing records
    /// </summary>
    public bool? CanUpdate { get; set; }
    
    /// <summary>
    /// Can delete records
    /// </summary>
    public bool? CanDelete { get; set; }
    
    /// <summary>
    /// Can export data
    /// </summary>
    public bool? CanExport { get; set; }
    
    /// <summary>
    /// Display in client menus
    /// </summary>
    public bool? DisplayInMenu { get; set; }
    
    /// <summary>
    /// Optional features (comma-separated)
    /// </summary>
    public string? Features { get; set; }
    
    /// <summary>
    /// Is the entitlement active
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// When updating a PROJECT entitlement, if true, will reset all child module overrides
    /// and cascade the new permission to all modules under this project.
    /// If false and there are child overrides, an error will be returned.
    /// </summary>
    public bool ResetChildOverrides { get; set; } = false;
}

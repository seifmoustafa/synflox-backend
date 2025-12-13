using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.PlanEntitlements;

/// <summary>
/// Request DTO for creating a new plan entitlement
/// </summary>
public class CreatePlanEntitlementRequest
{
    /// <summary>
    /// Plan to add entitlement to
    /// </summary>
    [Required]
    public Guid PlanId { get; set; }
    
    /// <summary>
    /// Target Project (mutually exclusive with ModuleId)
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    /// <summary>
    /// Target Module (mutually exclusive with ProjectId)
    /// </summary>
    public Guid? ModuleId { get; set; }
    
    /// <summary>
    /// Access level for this entitlement
    /// </summary>
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;
    
    /// <summary>
    /// Can create new records
    /// </summary>
    public bool CanCreate { get; set; } = true;
    
    /// <summary>
    /// Can read/view records
    /// </summary>
    public bool CanRead { get; set; } = true;
    
    /// <summary>
    /// Can update existing records
    /// </summary>
    public bool CanUpdate { get; set; } = true;
    
    /// <summary>
    /// Can delete records
    /// </summary>
    public bool CanDelete { get; set; } = true;
    
    /// <summary>
    /// Can export data
    /// </summary>
    public bool CanExport { get; set; } = true;
    
    /// <summary>
    /// Display in client menus
    /// </summary>
    public bool DisplayInMenu { get; set; } = true;
    
    /// <summary>
    /// Optional features (comma-separated)
    /// </summary>
    public string? Features { get; set; }
}

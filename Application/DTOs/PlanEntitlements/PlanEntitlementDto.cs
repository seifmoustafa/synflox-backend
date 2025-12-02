using System;
using Domain.Enums;

namespace Application.DTOs.PlanEntitlements;

/// <summary>
/// DTO for PlanEntitlement - represents access rights defined at the plan level
/// </summary>
public class PlanEntitlementDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    
    // Target
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? ModuleId { get; set; }
    public string? ModuleName { get; set; }
    
    // Hierarchy (for modules under a project)
    public Guid? ParentProjectId { get; set; }
    public string? ParentProjectName { get; set; }
    public bool IsOverride { get; set; }
    
    // Computed
    public string TargetType { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public bool IsModuleUnderProject { get; set; }
    public bool IsStandaloneModule { get; set; }
    public bool IsProjectEntitlement { get; set; }
    
    // Access Configuration
    public EntitlementAccessLevel AccessLevel { get; set; }
    public string AccessLevelDisplay { get; set; } = string.Empty;
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool DisplayInMenu { get; set; }
    public string? Features { get; set; }
    
    // Computed
    public bool HasFullAccess { get; set; }
    
    // Audit
    public bool IsActive { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
    public string? UpdatedBy { get; set; }
}

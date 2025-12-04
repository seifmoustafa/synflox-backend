using System;
using System.Collections.Generic;
using Application.DTOs.Subscriptions;

namespace Application.DTOs.PlanDto;

/// <summary>
/// Represents a module that conflicts because it's already in a project assigned to the plan
/// </summary>
public class PlanModuleConflictDto
{
    /// <summary>
    /// The conflicting module ID (encrypted)
    /// </summary>
    public Guid ModuleId { get; set; }
    
    /// <summary>
    /// The conflicting module name
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;
    
    /// <summary>
    /// The project that already contains this module
    /// </summary>
    public Guid ProjectId { get; set; }
    
    /// <summary>
    /// The project name that already contains this module
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
}

/// <summary>
/// Result of plan validation before create/update
/// </summary>
public class PlanValidationResultDto
{
    /// <summary>
    /// Whether the plan can be saved (no blocking errors)
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Whether there are warnings that require user confirmation
    /// </summary>
    public bool HasWarnings => ModuleConflicts.Any();
    
    /// <summary>
    /// Whether user confirmation is required before proceeding
    /// </summary>
    public bool RequiresConfirmation => HasWarnings;
    
    /// <summary>
    /// List of module conflicts (modules already in projects)
    /// </summary>
    public List<PlanModuleConflictDto> ModuleConflicts { get; set; } = new();
    
    /// <summary>
    /// List of module IDs that will be kept as standalone (no conflicts)
    /// </summary>
    public List<Guid> ValidStandaloneModuleIds { get; set; } = new();
    
    /// <summary>
    /// Human-readable warning message
    /// </summary>
    public string? WarningMessage { get; set; }
    
    /// <summary>
    /// Validation errors (blocking)
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Request to validate plan modules before save
/// </summary>
public class ValidatePlanModulesRequest
{
    /// <summary>
    /// Plan ID (for update operations, null for create)
    /// </summary>
    public Guid? PlanId { get; set; }
    
    /// <summary>
    /// Project IDs to be assigned to the plan
    /// </summary>
    public List<Guid> ProjectIds { get; set; } = new();
    
    /// <summary>
    /// Module IDs to be assigned as standalone modules
    /// </summary>
    public List<Guid> ModuleIds { get; set; } = new();
}

/// <summary>
/// Request to create plan with confirmation to remove duplicates
/// </summary>
public class CreatePlanWithConfirmationDto : CreateSubscriptionPlanDto
{
    /// <summary>
    /// User confirmed to remove duplicate modules that are already in projects
    /// </summary>
    public bool ConfirmRemoveDuplicates { get; set; }
}

/// <summary>
/// Request to update plan with confirmation to remove duplicates
/// </summary>
public class UpdatePlanWithConfirmationDto : UpdatePlanDto
{
    /// <summary>
    /// User confirmed to remove duplicate modules that are already in projects
    /// </summary>
    public bool ConfirmRemoveDuplicates { get; set; }
}

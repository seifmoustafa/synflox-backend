using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.PlanEntitlements;

/// <summary>
/// Response DTO for plan entitlement update operations.
/// Includes conflict information when updating project entitlements with child overrides.
/// </summary>
public class UpdatePlanEntitlementResponse
{
    /// <summary>
    /// Whether the update was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The updated entitlement (null if there are conflicts and update was not performed)
    /// </summary>
    public PlanEntitlementDto? Entitlement { get; set; }
    
    /// <summary>
    /// Whether there are child modules with overrides that need confirmation
    /// </summary>
    public bool HasChildOverrides { get; set; }
    
    /// <summary>
    /// Number of child modules with custom overrides
    /// </summary>
    public int OverrideCount { get; set; }
    
    /// <summary>
    /// List of module names with overrides (for display in confirmation dialog)
    /// </summary>
    public List<string> OverriddenModuleNames { get; set; } = new();
    
    /// <summary>
    /// Message to display to the user
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Create a success response
    /// </summary>
    public static UpdatePlanEntitlementResponse Succeeded(PlanEntitlementDto entitlement, string? message = null)
    {
        return new UpdatePlanEntitlementResponse
        {
            Success = true,
            Entitlement = entitlement,
            HasChildOverrides = false,
            OverrideCount = 0,
            Message = message
        };
    }
    
    /// <summary>
    /// Create a conflict response (requires confirmation)
    /// </summary>
    public static UpdatePlanEntitlementResponse Conflict(int overrideCount, List<string> moduleNames, string message)
    {
        return new UpdatePlanEntitlementResponse
        {
            Success = false,
            Entitlement = null,
            HasChildOverrides = true,
            OverrideCount = overrideCount,
            OverriddenModuleNames = moduleNames,
            Message = message
        };
    }
}

using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Project-level entitlement with modules
/// </summary>
public record ProjectEntitlementDto
{
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string? ProjectDescription { get; init; }
    public string? Icon { get; init; }
    
    /// <summary>
    /// Access level for the entire project
    /// </summary>
    public EntitlementAccessLevel AccessLevel { get; init; }
    
    /// <summary>
    /// Grant type (FullProject or SpecificModules)
    /// </summary>
    public EntitlementGrantType GrantType { get; init; }
    
    /// <summary>
    /// Allowed operations at project level
    /// </summary>
    public List<string> AllowedOperations { get; init; } = new();
    
    /// <summary>
    /// Modules within this project
    /// </summary>
    public List<ModuleEntitlementDto> Modules { get; set; } = new();
    
    /// <summary>
    /// Project-specific features
    /// </summary>
    public List<string> Features { get; init; } = new();
    
    /// <summary>
    /// When this project entitlement expires
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
    
    /// <summary>
    /// Whether to display in menu
    /// </summary>
    public bool DisplayInMenu { get; init; } = true;
}

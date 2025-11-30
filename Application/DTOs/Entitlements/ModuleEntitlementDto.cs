using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Module-level entitlement
/// </summary>
public record ModuleEntitlementDto
{
    public Guid ModuleId { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    public string? ModuleDescription { get; init; }
    public string? Icon { get; init; }
    
    /// <summary>
    /// Parent project (null for standalone modules)
    /// </summary>
    public Guid? ProjectId { get; init; }
    
    /// <summary>
    /// Access level for this module
    /// </summary>
    public EntitlementAccessLevel AccessLevel { get; init; }
    
    /// <summary>
    /// Allowed operations
    /// </summary>
    public List<string> AllowedOperations { get; init; } = new();
    
    /// <summary>
    /// Module-specific features enabled
    /// </summary>
    public List<string> Features { get; init; } = new();
    
    /// <summary>
    /// Usage limits for this module
    /// </summary>
    public Dictionary<string, object> UsageLimits { get; init; } = new();
    
    /// <summary>
    /// When this module entitlement expires
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
    
    /// <summary>
    /// Whether to display in menu
    /// </summary>
    public bool DisplayInMenu { get; init; } = true;
}

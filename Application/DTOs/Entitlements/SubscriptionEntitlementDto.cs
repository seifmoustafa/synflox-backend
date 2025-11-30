using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Response DTO for subscription entitlement display
/// </summary>
public record SubscriptionEntitlementDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    
    // References
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? ModuleId { get; set; }
    public string? ModuleName { get; set; }
    
    // Grant Configuration
    public EntitlementGrantType GrantType { get; set; }
    public EntitlementAccessLevel AccessLevel { get; set; }
    public EntitlementSource Source { get; set; }
    public bool IsCustom { get; set; }
    
    // CRUD Operations
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    
    // Features & Limits (parsed from JSON)
    public List<string> Features { get; set; } = new();
    public Dictionary<string, object> UsageLimits { get; set; } = new();
    
    // Display
    public string? Icon { get; set; }
    public bool DisplayInMenu { get; set; }
    public string? UpgradeCta { get; set; }
    public string? UpgradeUrl { get; set; }
    
    // Audit
    public Guid? GrantedByAdminId { get; set; }
    public string? GrantedByAdminName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    
    // Timestamps
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
    
    #region Computed Properties (DTO layer - OK per Clean Architecture)
    
    /// <summary>
    /// Is this entitlement currently valid?
    /// </summary>
    public bool IsValid => IsActive && (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.Now);
    
    /// <summary>
    /// Days until expiry (null if no expiry)
    /// </summary>
    public int? DaysUntilExpiry => ExpiresAt.HasValue 
        ? Math.Max(0, (int)(ExpiresAt.Value - DateTime.Now).TotalDays) 
        : null;
    
    /// <summary>
    /// Gets the list of allowed HTTP operations
    /// </summary>
    public List<string> AllowedOperations
    {
        get
        {
            var ops = new List<string>();
            if (CanRead) ops.Add("GET");
            if (CanCreate) ops.Add("POST");
            if (CanUpdate) ops.Add("PUT");
            if (CanDelete) ops.Add("DELETE");
            if (CanExport) ops.Add("EXPORT");
            return ops;
        }
    }
    
    /// <summary>
    /// Has full CRUD access
    /// </summary>
    public bool HasFullAccess => CanCreate && CanRead && CanUpdate && CanDelete && CanExport;
    
    /// <summary>
    /// Is read-only access
    /// </summary>
    public bool IsReadOnly => CanRead && !CanCreate && !CanUpdate && !CanDelete;
    
    /// <summary>
    /// Display name for the entitlement
    /// </summary>
    public string DisplayName => ModuleName ?? ProjectName ?? "Unknown";
    
    /// <summary>
    /// Human-readable access level description
    /// </summary>
    public string AccessLevelDisplay => AccessLevel switch
    {
        EntitlementAccessLevel.Full => "Full Access",
        EntitlementAccessLevel.ReadOnly => "Read Only",
        EntitlementAccessLevel.ExportOnly => "Export Only",
        EntitlementAccessLevel.Blocked => "Blocked",
        _ => "None"
    };
    
    /// <summary>
    /// Human-readable grant type description
    /// </summary>
    public string GrantTypeDisplay => GrantType switch
    {
        EntitlementGrantType.FullProject => "Full Project",
        EntitlementGrantType.SpecificModules => "Specific Modules",
        EntitlementGrantType.StandaloneModule => "Standalone Module",
        EntitlementGrantType.FeatureOnly => "Feature Only",
        _ => "None"
    };
    
    #endregion
}

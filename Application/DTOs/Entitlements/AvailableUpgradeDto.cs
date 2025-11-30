using System;
using System.Collections.Generic;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Available upgrade option (for marketing)
/// </summary>
public record AvailableUpgradeDto
{
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public Guid? ModuleId { get; init; }
    public string? ModuleName { get; init; }
    public string? Icon { get; init; }
    public string? Description { get; init; }
    
    /// <summary>
    /// Custom upgrade CTA
    /// </summary>
    public string UpgradeCta { get; init; } = "Upgrade Now";
    
    /// <summary>
    /// Upgrade URL
    /// </summary>
    public string UpgradeUrl { get; init; } = "/upgrade";
    
    /// <summary>
    /// Features available with this upgrade
    /// </summary>
    public List<string> IncludedFeatures { get; init; } = new();
}

using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// Aggregated view of a plan with all included projects, modules, and features
/// Used for detailed plan display in UI
/// </summary>
public class PlanDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// The type of duration for this plan (Weekly, Monthly, Yearly, Lifetime, etc.)
    /// </summary>
    public PlanDurationType DurationType { get; set; }
    
    /// <summary>
    /// Duration in months (kept for backward compatibility)
    /// </summary>
    public int DurationMonths { get; set; }
    
    /// <summary>
    /// Indicates if this is a lifetime/permanent plan
    /// </summary>
    public bool IsLifetimePlan { get; set; }
    
    /// <summary>
    /// Human-readable description of the duration (e.g., "1 month", "Lifetime (Never Expires)")
    /// </summary>
    public string DurationDescription { get; set; } = string.Empty;
    
    public bool AllowTrial { get; set; }
    public int? TrialDurationDays { get; set; }
    public bool AutoRenew { get; set; }
    public UpgradePolicy UpgradePolicy { get; set; }
    public int GracePeriodDays { get; set; }
    public List<string> CustomFeatures { get; set; } = new();
    public List<PlanPriceDto> Prices { get; set; } = new();
    
    /// <summary>
    /// Projects included in this plan with their features and modules
    /// </summary>
    public List<ProjectDto> Projects { get; set; } = new();
    
    /// <summary>
    /// Standalone modules included in this plan
    /// </summary>
    public List<ModuleDto> Modules { get; set; } = new();
}

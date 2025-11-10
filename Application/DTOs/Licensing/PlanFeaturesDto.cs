using System;
using System.Collections.Generic;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for plan features including inherited features from parent plans.
/// </summary>
public class PlanFeaturesDto
{
    /// <summary>
    /// The plan ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The plan name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Custom features specific to this plan.
    /// </summary>
    public string[] OwnFeatures { get; set; } = Array.Empty<string>();

    /// <summary>
    /// All features including inherited from parent plans.
    /// </summary>
    public string[] AllFeatures { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Project-modules included in this plan.
    /// </summary>
    public List<PlanProjectModuleDto> ProjectModules { get; set; } = new();

    /// <summary>
    /// Project-modules inherited from parent plans.
    /// </summary>
    public List<PlanProjectModuleDto> InheritedProjectModules { get; set; } = new();

    /// <summary>
    /// Parent plan information if this plan inherits features.
    /// </summary>
    public SubscriptionPlanDto? ParentPlan { get; set; }

    /// <summary>
    /// Child plans that inherit from this plan.
    /// </summary>
    public List<SubscriptionPlanDto> ChildPlans { get; set; } = new();
}

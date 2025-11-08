using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a many-to-many relationship between SubscriptionPlan and ProjectModule.
/// This allows subscription plans to include specific projects and modules.
/// </summary>
public class PlanProjectModule : AuditEntity<Guid>
{
    /// <summary>
    /// The ID of the subscription plan.
    /// </summary>
    public Guid SubscriptionPlanId { get; set; }

    /// <summary>
    /// Navigation property to the SubscriptionPlan.
    /// </summary>
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    /// <summary>
    /// The ID of the project-module relationship.
    /// </summary>
    public Guid ProjectModuleId { get; set; }

    /// <summary>
    /// Navigation property to the ProjectModule.
    /// </summary>
    public ProjectModule ProjectModule { get; set; } = null!;

    /// <summary>
    /// Indicates if this project-module is included in this plan.
    /// </summary>
    public bool IsIncluded { get; set; } = true;
}


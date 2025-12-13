using System;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Many-to-many relationship between Subscription Plans and Projects
/// Defines which projects are included in a plan
/// </summary>
public class PlanProject
{
    public Guid PlanId { get; set; }
    public Guid ProjectId { get; set; }

    // Navigation
    public SubscriptionPlan Plan { get; set; } = null!;
    public Project Project { get; set; } = null!;
}

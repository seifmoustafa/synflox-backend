using System;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Many-to-many relationship between Subscription Plans and Modules
/// Allows plans to include standalone modules without full projects
/// </summary>
public class PlanModule
{
    public Guid PlanId { get; set; }
    public Guid ModuleId { get; set; }

    // Navigation
    public SubscriptionPlan Plan { get; set; } = null!;
    public Module Module { get; set; } = null!;
}

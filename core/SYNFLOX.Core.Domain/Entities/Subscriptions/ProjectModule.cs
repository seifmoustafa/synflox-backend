using System;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Many-to-many relationship between Projects and Modules
/// Example: ERP Project includes HR, Accounting, Inventory modules
/// </summary>
public class ProjectModule
{
    public Guid ProjectId { get; set; }
    public Guid ModuleId { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
    public Module Module { get; set; } = null!;
}

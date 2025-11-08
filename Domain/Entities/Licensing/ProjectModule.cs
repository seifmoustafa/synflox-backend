using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a many-to-many relationship between Project and Module.
/// This allows modules to be associated with multiple projects.
/// </summary>
public class ProjectModule : AuditEntity<Guid>
{
    /// <summary>
    /// The ID of the project.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to the Project.
    /// </summary>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// The ID of the module.
    /// </summary>
    public Guid ModuleId { get; set; }

    /// <summary>
    /// Navigation property to the Module.
    /// </summary>
    public Module Module { get; set; } = null!;

    /// <summary>
    /// Indicates if this module is enabled for this project by default.
    /// </summary>
    public bool IsEnabledByDefault { get; set; } = true;

    /// <summary>
    /// Navigation property to the subscription plans that include this project-module.
    /// </summary>
    public ICollection<PlanProjectModule> PlanProjectModules { get; set; } = new List<PlanProjectModule>();
}


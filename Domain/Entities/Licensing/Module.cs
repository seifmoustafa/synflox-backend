using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a module/feature in the SYNFLOX licensing system (e.g., Inventory, Sales, HR, Reporting).
/// </summary>
public class Module : AuditEntity<Guid>
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property to the projects that include this module.
    /// </summary>
    public ICollection<ProjectModule> ProjectModules { get; set; } = new List<ProjectModule>();

    /// <summary>
    /// Navigation property to the subscription plans that include this module.
    /// </summary>
    public ICollection<PlanProjectModule> PlanProjectModules { get; set; } = new List<PlanProjectModule>();

    public bool IsActive { get; set; } = true;
}


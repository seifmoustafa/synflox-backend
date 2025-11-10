using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a project/product in the SYNFLOX licensing system (e.g., ERP, CRM, POS, HR).
/// </summary>
public class Project : AuditEntity<Guid>
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property to the modules available in this project.
    /// </summary>
    public ICollection<ProjectModule> ProjectModules { get; set; } = new List<ProjectModule>();

    /// <summary>
    /// Navigation property to the subscription plans that include this project.
    /// </summary>
    public ICollection<PlanProjectModule> PlanProjectModules { get; set; } = new List<PlanProjectModule>();

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON array of custom feature names for this project.
    /// </summary>
    [StringLength(2000)]
    public string? Features { get; set; }
}


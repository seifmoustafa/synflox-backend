using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a group/category for organizing companies.
/// </summary>
public class CompanyGroup : AuditEntity<Guid>
{
    /// <summary>
    /// Name of the group.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Description of the group.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the group is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}




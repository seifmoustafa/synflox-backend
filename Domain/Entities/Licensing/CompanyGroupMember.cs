using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing;

/// <summary>
/// Junction table for many-to-many relationship between Company and CompanyGroup.
/// </summary>
public class CompanyGroupMember : BaseEntity<Guid>
{
    /// <summary>
    /// The company ID.
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Navigation property to the company.
    /// </summary>
    public Company Company { get; set; } = null!;

    /// <summary>
    /// The company group ID.
    /// </summary>
    [Required]
    public Guid CompanyGroupId { get; set; }

    /// <summary>
    /// Navigation property to the company group.
    /// </summary>
    public CompanyGroup CompanyGroup { get; set; } = null!;
}




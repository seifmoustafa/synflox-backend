using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a custom field for a company.
/// </summary>
public class CompanyCustomField : AuditEntity<Guid>
{
    /// <summary>
    /// The company this field belongs to.
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Navigation property to the company.
    /// </summary>
    public Company Company { get; set; } = null!;

    /// <summary>
    /// Name of the custom field.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string FieldName { get; set; }

    /// <summary>
    /// Value of the custom field (stored as string, can be JSON for complex types).
    /// </summary>
    public string? FieldValue { get; set; }

    /// <summary>
    /// Type of the field (String, Number, Boolean, Date, JSON).
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string FieldType { get; set; } = "String";
}




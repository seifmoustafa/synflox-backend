using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for creating a company custom field.
/// </summary>
public class CreateCompanyCustomFieldDto
{
    /// <summary>
    /// Company ID (decrypted Guid). Set by controller from route parameter.
    /// </summary>
    public Guid CompanyId { get; set; }

    [Required(ErrorMessage = "Field name is required")]
    [StringLength(200, ErrorMessage = "Field name cannot exceed 200 characters")]
    public required string FieldName { get; set; }

    public string? FieldValue { get; set; }

    [Required(ErrorMessage = "Field type is required")]
    public CustomFieldType FieldType { get; set; } = CustomFieldType.String;
}




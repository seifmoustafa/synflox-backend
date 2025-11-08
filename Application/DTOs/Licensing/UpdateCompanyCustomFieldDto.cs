using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for updating a company custom field.
/// </summary>
public class UpdateCompanyCustomFieldDto
{
    [StringLength(200, ErrorMessage = "Field name cannot exceed 200 characters")]
    public string? FieldName { get; set; }

    public string? FieldValue { get; set; }

    public CustomFieldType? FieldType { get; set; }
}




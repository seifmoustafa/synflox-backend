using System;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for company custom field information.
/// </summary>
public class CompanyCustomFieldDto
{
    public Guid Id { get; set; } 
    public Guid CompanyId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? FieldValue { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}




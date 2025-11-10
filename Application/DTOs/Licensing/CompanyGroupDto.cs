using System;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for company group information.
/// </summary>
public class CompanyGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int CompaniesCount { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}

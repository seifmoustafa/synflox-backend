using System;

namespace Application.DTOs.Reporting;

/// <summary>
/// DTO for report definition information.
/// </summary>
public class ReportDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public bool IsPreBuilt { get; set; }
    public string? Parameters { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}



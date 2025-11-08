using System;
using System.Collections.Generic;

namespace Application.DTOs.Reporting;

/// <summary>
/// DTO for report results.
/// </summary>
public class ReportResultDto
{
    public string ReportName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public Dictionary<string, object> Summary { get; set; } = new();
}




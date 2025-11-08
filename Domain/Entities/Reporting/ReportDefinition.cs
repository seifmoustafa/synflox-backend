using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Reporting;

/// <summary>
/// Represents a report definition (pre-built or custom).
/// </summary>
public class ReportDefinition : AuditEntity<Guid>
{
    /// <summary>
    /// Name of the report.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Description of what the report shows.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Report type/category (e.g., "Subscription", "Usage", "Revenue").
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string ReportType { get; set; }

    /// <summary>
    /// Whether this is a pre-built report (true) or custom report (false).
    /// </summary>
    public bool IsPreBuilt { get; set; } = true;

    /// <summary>
    /// JSON object containing report parameters and their types.
    /// Example: {"fromDate": "DateTime", "toDate": "DateTime", "status": "string"}
    /// </summary>
    [StringLength(2000)]
    public string? Parameters { get; set; }

    /// <summary>
    /// Whether the report is active and can be generated.
    /// </summary>
    public bool IsActive { get; set; } = true;
}




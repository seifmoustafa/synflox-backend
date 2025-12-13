using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Represents a top-level product/system (e.g., ERP, CRM, POS, HR)
/// </summary>
public class Project : AuditEntity<Guid>
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// List of feature flags/capabilities provided by this project
    /// Examples: "MultiCurrency", "AdvancedReporting", "API Access"
    /// </summary>
    public List<string> Features { get; set; } = new();

    // Navigation properties
    public ICollection<ProjectModule> ProjectModules { get; set; } = new List<ProjectModule>();
    public ICollection<PlanProject> PlanProjects { get; set; } = new List<PlanProject>();
}

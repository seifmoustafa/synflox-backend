using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Represents a functional sub-unit within projects (e.g., HR, Accounting, Inventory, Sales)
/// </summary>
public class Module : AuditEntity<Guid>
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// List of feature flags/capabilities provided by this module
    /// Examples: "PayrollProcessing", "LeaveManagement", "AttendanceTracking"
    /// </summary>
    public List<string> Features { get; set; } = new();

    // Navigation properties
    public ICollection<ProjectModule> ProjectModules { get; set; } = new List<ProjectModule>();
    public ICollection<PlanModule> PlanModules { get; set; } = new List<PlanModule>();
}

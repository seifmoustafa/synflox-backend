using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Tenancy;

/// <summary>
/// Represents a tenant in a multi-tenant system.
/// </summary>
public class Tenant : AuditEntity<Guid>
{
    /// <summary>
    /// Name of the tenant.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Database connection string (for separate database mode).
    /// </summary>
    [StringLength(1000)]
    public string? DatabaseConnectionString { get; set; }

    /// <summary>
    /// Whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Tenant-specific settings as JSON.
    /// </summary>
    public string? Settings { get; set; }
}




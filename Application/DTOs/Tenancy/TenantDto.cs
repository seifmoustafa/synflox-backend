using System;

namespace Application.DTOs.Tenancy;

/// <summary>
/// DTO for tenant information.
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DatabaseConnectionString { get; set; }
    public bool IsActive { get; set; }
    public string? Settings { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}



using System;

namespace Application.DTOs.Authentication;

/// <summary>
/// DTO for API key information.
/// </summary>
public class ApiKeyDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string[]? AllowedIps { get; set; }
    public int? RateLimitPerHour { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}


using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication;

/// <summary>
/// Request DTO for updating an API key.
/// </summary>
public class UpdateApiKeyRequest
{
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string? Name { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string[]? AllowedIps { get; set; }

    [Range(1, 100000, ErrorMessage = "Rate limit must be between 1 and 100000")]
    public int? RateLimitPerHour { get; set; }

    public bool? IsActive { get; set; }
}




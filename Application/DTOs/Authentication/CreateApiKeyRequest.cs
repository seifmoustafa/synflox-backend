using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication;

/// <summary>
/// Request DTO for creating a new API key.
/// </summary>
public class CreateApiKeyRequest
{
    [Required(ErrorMessage = "Company ID is required")]
    public Guid CompanyId { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public required string Name { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string[]? AllowedIps { get; set; }

    [Range(1, 100000, ErrorMessage = "Rate limit must be between 1 and 100000")]
    public int? RateLimitPerHour { get; set; }
}


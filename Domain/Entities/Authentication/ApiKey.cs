using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Authentication;

/// <summary>
/// Represents an API key for external system authentication.
/// </summary>
public class ApiKey : AuditEntity<Guid>
{
    /// <summary>
    /// The company this API key belongs to.
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Navigation property to the company.
    /// </summary>
    public Domain.Entities.Licensing.Company Company { get; set; } = null!;

    /// <summary>
    /// Hashed API key (SHA256 hash of the actual key).
    /// </summary>
    [Required]
    [StringLength(64)] // SHA256 produces 64 hex characters
    public required string KeyHash { get; set; }

    /// <summary>
    /// First 8 characters of the key for display purposes (e.g., "sk_live_ab").
    /// </summary>
    [Required]
    [StringLength(20)]
    public required string KeyPrefix { get; set; }

    /// <summary>
    /// Human-readable name for the API key.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Whether the API key is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional expiration date for the API key.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Timestamp when the API key was last used (exact server local time).
    /// Null if the API key has never been used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// JSON array of allowed IP addresses. Empty array means allow from anywhere.
    /// </summary>
    [StringLength(2000)]
    public string? AllowedIps { get; set; }

    /// <summary>
    /// Rate limit per hour for this API key. Null means use default.
    /// </summary>
    public int? RateLimitPerHour { get; set; }

    /// <summary>
    /// Secret key used for HMAC request signing.
    /// This is separate from the API key itself and is used to sign requests.
    /// </summary>
    [StringLength(500)]
    public string? SigningSecret { get; set; }
}


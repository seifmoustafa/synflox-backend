using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Authentication;

/// <summary>
/// Represents a login attempt record for tracking authentication attempts.
/// </summary>
public class LoginAttempt : BaseEntity<Guid>
{
    /// <summary>
    /// The username that was attempted.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Username { get; set; }

    /// <summary>
    /// The IP address from which the attempt was made.
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// Whether the login attempt was successful.
    /// </summary>
    [Required]
    public bool Success { get; set; }

    /// <summary>
    /// Reason for failure if the attempt was unsuccessful.
    /// </summary>
    [StringLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Timestamp when the attempt was made.
    /// </summary>
    [Required]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The admin ID if login was successful.
    /// </summary>
    public Guid? AdminId { get; set; }
}


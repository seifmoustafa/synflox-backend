using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Authentication;

/// <summary>
/// Tracks password history for preventing password reuse.
/// </summary>
public class PasswordHistory : BaseEntity<Guid>
{
    /// <summary>
    /// The ID of the admin whose password is tracked.
    /// </summary>
    public Guid AdminId { get; set; }

    /// <summary>
    /// Navigation property to the Admin.
    /// </summary>
    public Admin Admin { get; set; } = null!;

    /// <summary>
    /// The hashed password.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// The timestamp when this password was set.
    /// </summary>
    public DateTime SetAt { get; set; } = DateTime.UtcNow;
}


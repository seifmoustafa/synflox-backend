using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Settings;

/// <summary>
/// Represents the password policy configuration for the system.
/// </summary>
public class PasswordPolicy : AuditEntity<Guid>
{
    /// <summary>
    /// Minimum password length.
    /// </summary>
    [Range(6, 128, ErrorMessage = "Minimum length must be between 6 and 128")]
    public int MinLength { get; set; } = 8;

    /// <summary>
    /// Requires at least one uppercase letter.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Requires at least one lowercase letter.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Requires at least one number.
    /// </summary>
    public bool RequireNumbers { get; set; } = true;

    /// <summary>
    /// Requires at least one special character.
    /// </summary>
    public bool RequireSpecialChars { get; set; } = true;

    /// <summary>
    /// Maximum password age in days. Null means passwords never expire.
    /// </summary>
    public int? MaxAgeDays { get; set; } = 90;

    /// <summary>
    /// Number of previous passwords to prevent reuse. Null means no restriction.
    /// </summary>
    public int? PreventReuseCount { get; set; } = 5;

    /// <summary>
    /// Indicates if this policy is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}


using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

/// <summary>
/// Settings for login security and account lockout.
/// </summary>
public class LoginSecuritySettings
{
    /// <summary>
    /// Maximum number of failed login attempts before account lockout.
    /// </summary>
    [Range(1, 20)]
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// </summary>
    [Range(1, 1440)] // 1 minute to 24 hours
    public int LockoutDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Time window in minutes for counting failed attempts.
    /// </summary>
    [Range(1, 1440)]
    public int FailedAttemptWindowMinutes { get; set; } = 30;
}


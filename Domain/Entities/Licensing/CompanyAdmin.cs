using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a client administrator account for a company.
/// Allows human admins to login with username/password to manage devices.
/// Each company has ONE admin account (can be changed/reset by SYNFLOX admin).
/// Admin is ALWAYS allowed to access - never counted in device limits.
/// </summary>
public class CompanyAdmin : AuditEntity<Guid>
{
    #region Company Relationship

    /// <summary>
    /// The company this admin belongs to.
    /// One admin per company.
    /// </summary>
    public Guid CompanyId { get; set; }

    #endregion

    #region Credentials

    /// <summary>
    /// Unique username for login.
    /// Recommended format: company email or company-specific identifier.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Username { get; set; }

    /// <summary>
    /// Hashed password (using BCrypt or similar).
    /// Never store plain text passwords.
    /// </summary>
    [Required]
    [StringLength(255)]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Salt used for password hashing.
    /// Each admin has unique salt.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Salt { get; set; }

    #endregion

    #region Profile

    /// <summary>
    /// Display name shown in UI.
    /// Example: "John Smith", "IT Department".
    /// </summary>
    [Required]
    [StringLength(150)]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Email address for notifications and password reset.
    /// </summary>
    [Required]
    [StringLength(200)]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// Phone number for 2FA or support contact.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Phone { get; set; }

    #endregion

    #region Account Status

    /// <summary>
    /// Whether this admin account is active.
    /// Inactive accounts cannot login.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the admin must change password on next login.
    /// Set to true when account is created or password is reset by SYNFLOX admin.
    /// </summary>
    public bool MustChangePassword { get; set; } = true;

    /// <summary>
    /// Number of consecutive failed login attempts.
    /// Reset to 0 on successful login.
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Maximum failed attempts before account is locked.
    /// 0 = no lockout.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// When the account is locked until (UTC).
    /// MinValue = not locked.
    /// </summary>
    public DateTime LockedUntilUtc { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Duration of lockout in minutes after max failed attempts.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 30;

    /// <summary>
    /// When the password was last changed (UTC).
    /// </summary>
    public DateTime PasswordChangedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Password expiry in days.
    /// 0 = never expires.
    /// </summary>
    public int PasswordExpiryDays { get; set; } = 0;

    #endregion

    #region Session Configuration (Admin Configurable)

    /// <summary>
    /// How admin sessions are managed.
    /// Configurable by the admin.
    /// </summary>
    public AdminSessionPolicy SessionPolicy { get; set; } = AdminSessionPolicy.SingleSession;

    /// <summary>
    /// Session timeout in minutes.
    /// After this period of inactivity, session expires.
    /// </summary>
    [Range(15, 1440)] // 15 min to 24 hours
    public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours default

    /// <summary>
    /// Whether to auto-logout after inactivity.
    /// </summary>
    public bool AutoLogoutOnInactivity { get; set; } = true;

    /// <summary>
    /// Inactivity timeout in minutes before auto-logout.
    /// Only applies when AutoLogoutOnInactivity = true.
    /// </summary>
    [Range(5, 480)] // 5 min to 8 hours
    public int InactivityTimeoutMinutes { get; set; } = 30;

    #endregion

    #region Current Session (for SingleSession enforcement)

    /// <summary>
    /// Current active session ID.
    /// Empty string if no active session.
    /// </summary>
    [StringLength(100)]
    public string CurrentSessionId { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the device currently logged in.
    /// Used for session validation.
    /// </summary>
    [StringLength(128)]
    public string CurrentDeviceHash { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name of current device.
    /// </summary>
    [StringLength(200)]
    public string CurrentDeviceName { get; set; } = string.Empty;

    /// <summary>
    /// IP address of current session.
    /// </summary>
    [StringLength(45)]
    public string CurrentSessionIp { get; set; } = string.Empty;

    /// <summary>
    /// When the current session started (UTC).
    /// </summary>
    public DateTime SessionStartedAtUtc { get; set; }

    /// <summary>
    /// When the admin last performed an action (UTC).
    /// Used for inactivity timeout.
    /// </summary>
    public DateTime LastActivityAtUtc { get; set; }

    #endregion

    #region Permissions

    /// <summary>
    /// Can manage (bind/unbind) devices.
    /// </summary>
    public bool CanManageDevices { get; set; } = true;

    /// <summary>
    /// Can view subscription details and status.
    /// </summary>
    public bool CanViewSubscriptions { get; set; } = true;

    /// <summary>
    /// Can approve device replacement requests.
    /// </summary>
    public bool CanApproveReplacements { get; set; } = true;

    /// <summary>
    /// Can generate/regenerate offline license keys.
    /// </summary>
    public bool CanGenerateLicenses { get; set; } = true;

    /// <summary>
    /// Can view usage reports and analytics.
    /// </summary>
    public bool CanViewUsageReports { get; set; } = true;

    /// <summary>
    /// Can modify their own session settings.
    /// </summary>
    public bool CanModifySessionSettings { get; set; } = true;

    #endregion

    #region Audit

    /// <summary>
    /// When the admin last logged in (UTC).
    /// </summary>
    public DateTime LastLoginAtUtc { get; set; }

    /// <summary>
    /// IP address of last login.
    /// </summary>
    [StringLength(45)]
    public string LastLoginIp { get; set; } = string.Empty;

    /// <summary>
    /// Total number of successful logins.
    /// </summary>
    public int TotalLogins { get; set; } = 0;

    #endregion

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<CompanyAdminSession> Sessions { get; set; } = new List<CompanyAdminSession>();

    // Computed properties

    /// <summary>
    /// Whether the account is currently locked.
    /// </summary>
    public bool IsLocked => LockedUntilUtc > DateTime.MinValue && LockedUntilUtc > DateTime.UtcNow;

    /// <summary>
    /// Whether the password has expired.
    /// </summary>
    public bool IsPasswordExpired => PasswordExpiryDays > 0 && 
        PasswordChangedAtUtc > DateTime.MinValue && 
        PasswordChangedAtUtc.AddDays(PasswordExpiryDays) < DateTime.UtcNow;

    /// <summary>
    /// Whether there is an active session.
    /// </summary>
    public bool HasActiveSession => !string.IsNullOrEmpty(CurrentSessionId) && 
        SessionStartedAtUtc > DateTime.MinValue;

    /// <summary>
    /// Whether the current session has timed out.
    /// </summary>
    public bool IsSessionTimedOut => SessionStartedAtUtc > DateTime.MinValue && 
        SessionStartedAtUtc.AddMinutes(SessionTimeoutMinutes) < DateTime.UtcNow;

    /// <summary>
    /// Whether the session is inactive (for auto-logout).
    /// </summary>
    public bool IsSessionInactive => AutoLogoutOnInactivity && 
        LastActivityAtUtc > DateTime.MinValue && 
        LastActivityAtUtc.AddMinutes(InactivityTimeoutMinutes) < DateTime.UtcNow;
}

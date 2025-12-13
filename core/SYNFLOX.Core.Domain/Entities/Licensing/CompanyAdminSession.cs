using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing;

/// <summary>
/// Tracks individual login sessions for a company admin.
/// Used for:
/// - Session history and audit trail
/// - Multi-session management (when policy allows)
/// - Security monitoring and anomaly detection
/// </summary>
public class CompanyAdminSession : AuditEntity<Guid>
{
    /// <summary>
    /// The admin this session belongs to.
    /// </summary>
    public Guid AdminId { get; set; }

    /// <summary>
    /// Unique session identifier.
    /// Used for session validation and JWT correlation.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string SessionId { get; set; }

    /// <summary>
    /// Hash of the device used for this session.
    /// </summary>
    [Required]
    [StringLength(128)]
    public required string DeviceHash { get; set; }

    /// <summary>
    /// Friendly name of the device.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string DeviceName { get; set; }

    /// <summary>
    /// Operating system of the device.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string OperatingSystem { get; set; }

    /// <summary>
    /// Browser/client information.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string UserAgent { get; set; }

    /// <summary>
    /// IP address at session start.
    /// </summary>
    [Required]
    [StringLength(45)]
    public required string IpAddress { get; set; }

    /// <summary>
    /// Geographic location (if determinable from IP).
    /// Example: "Cairo, Egypt".
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Location { get; set; }

    /// <summary>
    /// When the session started (UTC).
    /// </summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// When the session expires (UTC).
    /// Based on admin's SessionTimeoutMinutes.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// When the session ended (UTC).
    /// MinValue if session is still active.
    /// </summary>
    public DateTime EndedAtUtc { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Last activity timestamp (UTC).
    /// Updated on each action.
    /// </summary>
    public DateTime LastActivityAtUtc { get; set; }

    /// <summary>
    /// Whether the session is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// How the session ended.
    /// </summary>
    public SessionEndReason EndReason { get; set; } = SessionEndReason.Logout;

    /// <summary>
    /// Additional notes about session termination.
    /// </summary>
    [StringLength(500)]
    public string EndNotes { get; set; } = string.Empty;

    /// <summary>
    /// Total number of actions performed in this session.
    /// </summary>
    public int ActionCount { get; set; } = 0;

    // Navigation properties
    public virtual CompanyAdmin Admin { get; set; } = null!;

    // Computed properties

    /// <summary>
    /// Duration of the session in minutes.
    /// </summary>
    public double DurationMinutes => EndedAtUtc > DateTime.MinValue 
        ? (EndedAtUtc - StartedAtUtc).TotalMinutes 
        : (DateTime.UtcNow - StartedAtUtc).TotalMinutes;

    /// <summary>
    /// Whether the session has expired.
    /// </summary>
    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;

    /// <summary>
    /// Whether the session is valid (active and not expired).
    /// </summary>
    public bool IsValid => IsActive && !IsExpired && !IsDeleted;
}

/// <summary>
/// Reason for session termination.
/// </summary>
public enum SessionEndReason
{
    /// <summary>
    /// Admin manually logged out.
    /// </summary>
    Logout = 1,

    /// <summary>
    /// Session expired due to timeout.
    /// </summary>
    Timeout = 2,

    /// <summary>
    /// Session ended due to inactivity.
    /// </summary>
    Inactivity = 3,

    /// <summary>
    /// Session terminated because new session started (SingleSession policy).
    /// </summary>
    NewSessionStarted = 4,

    /// <summary>
    /// Session terminated by SYNFLOX admin.
    /// </summary>
    AdminTerminated = 5,

    /// <summary>
    /// Session terminated due to password change.
    /// </summary>
    PasswordChanged = 6,

    /// <summary>
    /// Session terminated due to account deactivation.
    /// </summary>
    AccountDeactivated = 7,

    /// <summary>
    /// Session terminated due to security concern.
    /// </summary>
    SecurityConcern = 8
}

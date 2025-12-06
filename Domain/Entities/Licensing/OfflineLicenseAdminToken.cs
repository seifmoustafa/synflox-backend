using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a JWT token for client administrators to manage their offline license devices.
/// This token is per-company and allows the client admin to:
/// - Bind/unbind devices to their subscriptions
/// - View bound devices
/// - Approve device replacement requests
/// This is separate from ClientAccessToken which is for subscription/license validation.
/// </summary>
public class OfflineLicenseAdminToken : AuditEntity<Guid>
{
    /// <summary>
    /// The company this admin token belongs to.
    /// One token per company for managing all their subscriptions' devices.
    /// </summary>
    public Guid CompanyId { get; set; }
    
    /// <summary>
    /// Navigation property to Company
    /// </summary>
    public virtual Company? Company { get; set; }

    /// <summary>
    /// Friendly name for this token (e.g., "IT Admin Token", "Production Token")
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// SHA-256 hash of the JWT token for security and lookup.
    /// We never store the actual token.
    /// </summary>
    [Required]
    [StringLength(64)] // SHA-256 produces 64-character hex string
    public required string TokenHash { get; set; }

    /// <summary>
    /// When the token was issued (UTC)
    /// </summary>
    public DateTime IssuedAtUtc { get; set; }

    /// <summary>
    /// When the token expires (UTC)
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Current status of the token
    /// </summary>
    public ClientTokenStatus Status { get; set; } = ClientTokenStatus.Active;

    /// <summary>
    /// Reason for revocation (if revoked)
    /// </summary>
    [StringLength(500)]
    public string? RevocationReason { get; set; }

    /// <summary>
    /// When the token was revoked (UTC)
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// When the token was last used (UTC)
    /// </summary>
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>
    /// Total number of times this token has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Client IP address that last used this token
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? LastUsedFromIp { get; set; }

    /// <summary>
    /// User agent of the client that last used this token
    /// </summary>
    [StringLength(500)]
    public string? LastUsedUserAgent { get; set; }

    #region Permissions

    /// <summary>
    /// Can bind new devices to subscriptions
    /// </summary>
    public bool CanBindDevices { get; set; } = true;

    /// <summary>
    /// Can unbind/remove devices from subscriptions
    /// </summary>
    public bool CanUnbindDevices { get; set; } = true;

    /// <summary>
    /// Can view list of bound devices
    /// </summary>
    public bool CanViewDevices { get; set; } = true;

    /// <summary>
    /// Can approve device replacement requests
    /// </summary>
    public bool CanApproveReplacements { get; set; } = true;

    #endregion

    #region Usage Limits

    /// <summary>
    /// Maximum number of API calls allowed per day (0 = unlimited)
    /// </summary>
    public int DailyApiLimit { get; set; } = 0;

    /// <summary>
    /// API calls made today
    /// </summary>
    public int TodayApiCalls { get; set; } = 0;

    /// <summary>
    /// Date of last API call count reset
    /// </summary>
    public DateTime? LastApiCountResetDate { get; set; }

    #endregion

    /// <summary>
    /// Token version for future compatibility
    /// </summary>
    [StringLength(10)]
    public string TokenVersion { get; set; } = "1.0";

    /// <summary>
    /// Notes about this token (internal use)
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    // Computed properties
    
    /// <summary>
    /// Whether the token is currently valid (active and not expired)
    /// </summary>
    public bool IsValid => Status == ClientTokenStatus.Active && 
                          ExpiresAtUtc > DateTime.UtcNow && 
                          !IsDeleted;

    /// <summary>
    /// Whether the token has expired
    /// </summary>
    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;

    /// <summary>
    /// Days until expiry (negative if expired)
    /// </summary>
    public int DaysUntilExpiry => (int)(ExpiresAtUtc - DateTime.UtcNow).TotalDays;

    /// <summary>
    /// Check if rate limit exceeded for today
    /// </summary>
    public bool IsRateLimitExceeded => DailyApiLimit > 0 && TodayApiCalls >= DailyApiLimit;

    /// <summary>
    /// Reset daily API count if needed
    /// </summary>
    public void ResetDailyCountIfNeeded()
    {
        var today = DateTime.UtcNow.Date;
        if (LastApiCountResetDate == null || LastApiCountResetDate.Value.Date < today)
        {
            TodayApiCalls = 0;
            LastApiCountResetDate = today;
        }
    }

    /// <summary>
    /// Record an API call
    /// </summary>
    public void RecordApiCall(string? ipAddress = null, string? userAgent = null)
    {
        ResetDailyCountIfNeeded();
        TodayApiCalls++;
        UsageCount++;
        LastUsedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(ipAddress)) LastUsedFromIp = ipAddress;
        if (!string.IsNullOrEmpty(userAgent)) LastUsedUserAgent = userAgent;
    }
}

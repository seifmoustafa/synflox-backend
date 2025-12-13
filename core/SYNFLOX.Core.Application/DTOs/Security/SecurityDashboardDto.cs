using System;
using System.Collections.Generic;

namespace Application.DTOs.Security
{
    /// <summary>
    /// Comprehensive security analytics dashboard data
    /// Provides overview of security status and recent activity
    /// </summary>
    public class SecurityDashboardDto
    {
        /// <summary>
        /// Overall security score (0-100)
        /// Based on 2FA usage, backup codes, recent security events
        /// </summary>
        public int SecurityScore { get; set; }

        /// <summary>
        /// Security level: Critical, Low, Medium, High, Excellent
        /// </summary>
        public required string SecurityLevel { get; set; }

        /// <summary>
        /// Two-Factor Authentication statistics
        /// </summary>
        public required TwoFactorStatsDto TwoFactorStats { get; set; }

        /// <summary>
        /// Backup codes statistics
        /// </summary>
        public required BackupCodesStatsDto BackupCodesStats { get; set; }

        /// <summary>
        /// Recent security events (last 7 days)
        /// </summary>
        public required List<SecurityEventDto> RecentEvents { get; set; }

        /// <summary>
        /// Failed login attempts statistics
        /// </summary>
        public required FailedLoginStatsDto FailedLoginStats { get; set; }

        /// <summary>
        /// Security recommendations for user
        /// </summary>
        public required List<string> Recommendations { get; set; }

        /// <summary>
        /// Last security audit timestamp
        /// </summary>
        public DateTime LastAuditDate { get; set; }
    }

    /// <summary>
    /// Two-Factor Authentication statistics
    /// </summary>
    public class TwoFactorStatsDto
    {
        /// <summary>
        /// Whether 2FA is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Date when 2FA was enabled (null if not enabled)
        /// </summary>
        public DateTime? EnabledDate { get; set; }

        /// <summary>
        /// Last 2FA verification timestamp
        /// </summary>
        public DateTime? LastVerification { get; set; }

        /// <summary>
        /// Total 2FA verifications (lifetime)
        /// </summary>
        public int TotalVerifications { get; set; }

        /// <summary>
        /// Failed 2FA attempts in last 30 days
        /// </summary>
        public int FailedAttemptsLast30Days { get; set; }
    }

    /// <summary>
    /// Backup codes statistics
    /// </summary>
    public class BackupCodesStatsDto
    {
        /// <summary>
        /// Total backup codes generated
        /// </summary>
        public int TotalGenerated { get; set; }

        /// <summary>
        /// Remaining available codes
        /// </summary>
        public int RemainingCodes { get; set; }

        /// <summary>
        /// Used codes count
        /// </summary>
        public int UsedCodes { get; set; }

        /// <summary>
        /// Expired codes count
        /// </summary>
        public int ExpiredCodes { get; set; }

        /// <summary>
        /// Last generation date
        /// </summary>
        public DateTime? LastGenerationDate { get; set; }

        /// <summary>
        /// Next expiry date
        /// </summary>
        public DateTime? NextExpiryDate { get; set; }

        /// <summary>
        /// Days until next code expires
        /// </summary>
        public int? DaysUntilExpiry { get; set; }

        /// <summary>
        /// Whether codes need regeneration
        /// </summary>
        public bool NeedsRegeneration { get; set; }
    }

    /// <summary>
    /// Security event summary
    /// </summary>
    public class SecurityEventDto
    {
        /// <summary>
        /// Event type (e.g., "2FAEnabled", "BackupCodeUsed")
        /// </summary>
        public required string EventType { get; set; }

        /// <summary>
        /// Human-readable event description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// IP address where event occurred
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Whether the event was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Severity level: Info, Warning, Critical
        /// </summary>
        public required string Severity { get; set; }
    }

    /// <summary>
    /// Failed login attempts statistics
    /// </summary>
    public class FailedLoginStatsDto
    {
        /// <summary>
        /// Failed attempts in last 24 hours
        /// </summary>
        public int Last24Hours { get; set; }

        /// <summary>
        /// Failed attempts in last 7 days
        /// </summary>
        public int Last7Days { get; set; }

        /// <summary>
        /// Failed attempts in last 30 days
        /// </summary>
        public int Last30Days { get; set; }

        /// <summary>
        /// Most recent failed attempt timestamp
        /// </summary>
        public DateTime? MostRecentAttempt { get; set; }

        /// <summary>
        /// Whether there are suspicious patterns
        /// </summary>
        public bool SuspiciousActivity { get; set; }

        /// <summary>
        /// List of IP addresses with failed attempts
        /// </summary>
        public required List<string> SuspiciousIps { get; set; }
    }
}

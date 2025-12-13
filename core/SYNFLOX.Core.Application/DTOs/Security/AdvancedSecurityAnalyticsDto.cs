using System;
using System.Collections.Generic;

namespace Application.DTOs.Security
{
    /// <summary>
    /// Advanced security analytics with time-series data and trends
    /// </summary>
    public class AdvancedSecurityAnalyticsDto
    {
        /// <summary>
        /// Security score trend (last 30 days)
        /// </summary>
        public required List<SecurityScoreTrendDto> SecurityScoreTrend { get; set; }

        /// <summary>
        /// Login activity analytics
        /// </summary>
        public required LoginActivityAnalyticsDto LoginActivity { get; set; }

        /// <summary>
        /// 2FA usage analytics
        /// </summary>
        public required TwoFactorAnalyticsDto TwoFactorAnalytics { get; set; }

        /// <summary>
        /// Backup codes usage analytics
        /// </summary>
        public required BackupCodesAnalyticsDto BackupCodesAnalytics { get; set; }

        /// <summary>
        /// Geographic distribution of logins
        /// </summary>
        public required List<GeographicLoginDto> GeographicDistribution { get; set; }

        /// <summary>
        /// Time-based activity patterns (hourly distribution)
        /// </summary>
        public required List<HourlyActivityDto> ActivityByHour { get; set; }

        /// <summary>
        /// Device/browser analytics
        /// </summary>
        public required List<DeviceAnalyticsDto> DeviceDistribution { get; set; }

        /// <summary>
        /// Threat level assessment
        /// </summary>
        public required ThreatLevelDto ThreatLevel { get; set; }
    }

    /// <summary>
    /// Security score at a point in time
    /// </summary>
    public class SecurityScoreTrendDto
    {
        public DateTime Date { get; set; }
        public int Score { get; set; }
        public required string Level { get; set; }
    }

    /// <summary>
    /// Login activity analytics
    /// </summary>
    public class LoginActivityAnalyticsDto
    {
        public int TotalLogins { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLogins { get; set; }
        public double SuccessRate { get; set; }
        public required List<DailyLoginStatsDto> DailyStats { get; set; }
        public required List<string> TopIpAddresses { get; set; }
    }

    /// <summary>
    /// Daily login statistics
    /// </summary>
    public class DailyLoginStatsDto
    {
        public DateTime Date { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }

    /// <summary>
    /// Two-factor authentication analytics
    /// </summary>
    public class TwoFactorAnalyticsDto
    {
        public int TotalVerifications { get; set; }
        public int SuccessfulVerifications { get; set; }
        public int FailedVerifications { get; set; }
        public double SuccessRate { get; set; }
        public int BackupCodeUsageCount { get; set; }
        public required List<Daily2FAStatsDto> DailyStats { get; set; }
    }

    /// <summary>
    /// Daily 2FA statistics
    /// </summary>
    public class Daily2FAStatsDto
    {
        public DateTime Date { get; set; }
        public int VerificationCount { get; set; }
        public int FailureCount { get; set; }
    }

    /// <summary>
    /// Backup codes usage analytics
    /// </summary>
    public class BackupCodesAnalyticsDto
    {
        public int TotalGenerated { get; set; }
        public int TotalUsed { get; set; }
        public int TotalExpired { get; set; }
        public double UsageRate { get; set; }
        public required List<BackupCodeUsageDto> UsageHistory { get; set; }
    }

    /// <summary>
    /// Backup code usage event
    /// </summary>
    public class BackupCodeUsageDto
    {
        public DateTime Date { get; set; }
        public required string Action { get; set; } // "generated", "used", "expired"
        public int Count { get; set; }
    }

    /// <summary>
    /// Geographic login distribution
    /// </summary>
    public class GeographicLoginDto
    {
        public required string IpAddress { get; set; }
        public required string Country { get; set; }
        public required string City { get; set; }
        public int LoginCount { get; set; }
        public DateTime LastLogin { get; set; }
    }

    /// <summary>
    /// Hourly activity distribution
    /// </summary>
    public class HourlyActivityDto
    {
        public int Hour { get; set; } // 0-23
        public int ActivityCount { get; set; }
    }

    /// <summary>
    /// Device/browser analytics
    /// </summary>
    public class DeviceAnalyticsDto
    {
        public required string UserAgent { get; set; }
        public required string DeviceType { get; set; } // "Desktop", "Mobile", "Tablet"
        public required string Browser { get; set; }
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// Threat level assessment
    /// </summary>
    public class ThreatLevelDto
    {
        public required string Level { get; set; } // "None", "Low", "Medium", "High", "Critical"
        public int Score { get; set; } // 0-100
        public required List<string> Threats { get; set; }
        public required List<string> Recommendations { get; set; }
    }
}

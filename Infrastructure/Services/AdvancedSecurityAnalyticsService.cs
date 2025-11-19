using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Security;
using Application.Services;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// Service for advanced security analytics
    /// </summary>
    public class AdvancedSecurityAnalyticsService : IAdvancedSecurityAnalyticsService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly ISecurityAuditLogRepository _auditLogRepository;
        private readonly ILocalizationService _localizer;
        private readonly IIpGeolocationService _ipGeolocationService;
        private readonly IUserAgentParserService _userAgentParser;

        public AdvancedSecurityAnalyticsService(
            IAdminRepository adminRepository,
            ISecurityAuditLogRepository auditLogRepository,
            ILocalizationService localizer,
            IIpGeolocationService ipGeolocationService,
            IUserAgentParserService userAgentParser)
        {
            _adminRepository = adminRepository;
            _auditLogRepository = auditLogRepository;
            _localizer = localizer;
            _ipGeolocationService = ipGeolocationService;
            _userAgentParser = userAgentParser;
        }

        public async Task<AdvancedSecurityAnalyticsDto> GetAdvancedAnalyticsAsync(Guid adminId, DateTime startDate, DateTime endDate)
        {
            // Verify admin exists
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null || admin.IsDeleted)
            {
                throw new NotFoundException(_localizer["Admin.NotFound"]);
            }

            // Get all events in date range
            var events = await _auditLogRepository.GetRecentByAdminAsync(adminId, startDate, 1000);
            events = events.Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate).ToList();

            // Calculate security score trend (daily)
            var scoreTrend = CalculateSecurityScoreTrend(events, startDate, endDate);

            // Analyze login activity
            var loginActivity = AnalyzeLoginActivity(events);

            // Analyze 2FA usage
            var twoFactorAnalytics = AnalyzeTwoFactorUsage(events);

            // Analyze backup codes
            var backupCodesAnalytics = AnalyzeBackupCodesUsage(events);

            // Geographic distribution (REAL - using IP geolocation)
            var geographicDistribution = await AnalyzeGeographicDistributionAsync(events);

            // Activity by hour
            var activityByHour = AnalyzeActivityByHour(events);

            // Device distribution (REAL - using user-agent parsing)
            var deviceDistribution = AnalyzeDeviceDistribution(events);

            // Threat level assessment
            var threatLevel = AssessThreatLevel(events, loginActivity);

            return new AdvancedSecurityAnalyticsDto
            {
                SecurityScoreTrend = scoreTrend,
                LoginActivity = loginActivity,
                TwoFactorAnalytics = twoFactorAnalytics,
                BackupCodesAnalytics = backupCodesAnalytics,
                GeographicDistribution = geographicDistribution,
                ActivityByHour = activityByHour,
                DeviceDistribution = deviceDistribution,
                ThreatLevel = threatLevel
            };
        }

        private List<SecurityScoreTrendDto> CalculateSecurityScoreTrend(List<Domain.Entities.Authentication.SecurityAuditLog> events, DateTime start, DateTime end)
        {
            var trend = new List<SecurityScoreTrendDto>();
            var currentDate = start.Date;

            while (currentDate <= end.Date)
            {
                // Simplified scoring - in production, would use actual security metrics
                var dayEvents = events.Where(e => e.CreatedAt.Date == currentDate).ToList();
                var score = 70; // Base score

                // Adjust based on events
                if (dayEvents.Any(e => e.EventType == "2FAEnabled")) score += 15;
                if (dayEvents.Any(e => e.EventType.Contains("Failed"))) score -= 10;
                if (dayEvents.Count(e => !e.Success) > 5) score -= 15;

                score = Math.Max(0, Math.Min(100, score));

                trend.Add(new SecurityScoreTrendDto
                {
                    Date = currentDate,
                    Score = score,
                    Level = GetSecurityLevel(score)
                });

                currentDate = currentDate.AddDays(1);
            }

            return trend;
        }

        private LoginActivityAnalyticsDto AnalyzeLoginActivity(List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            var loginEvents = events.Where(e => e.EventType.Contains("Login")).ToList();
            var successful = loginEvents.Count(e => e.Success);
            var failed = loginEvents.Count(e => !e.Success);

            var dailyStats = loginEvents
                .GroupBy(e => e.CreatedAt.Date)
                .Select(g => new DailyLoginStatsDto
                {
                    Date = g.Key,
                    SuccessCount = g.Count(e => e.Success),
                    FailureCount = g.Count(e => !e.Success)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var topIps = loginEvents
                .Where(e => !string.IsNullOrEmpty(e.IpAddress))
                .GroupBy(e => e.IpAddress)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key!)
                .ToList();

            return new LoginActivityAnalyticsDto
            {
                TotalLogins = loginEvents.Count,
                SuccessfulLogins = successful,
                FailedLogins = failed,
                SuccessRate = loginEvents.Count > 0 ? (double)successful / loginEvents.Count * 100 : 100,
                DailyStats = dailyStats,
                TopIpAddresses = topIps
            };
        }

        private TwoFactorAnalyticsDto AnalyzeTwoFactorUsage(List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            var twoFactorEvents = events.Where(e => e.EventType.Contains("TwoFactor")).ToList();
            var successful = twoFactorEvents.Count(e => e.Success);
            var failed = twoFactorEvents.Count(e => !e.Success);
            var backupCodeUsage = events.Count(e => e.EventType == "BackupCodeUsed");

            var dailyStats = twoFactorEvents
                .GroupBy(e => e.CreatedAt.Date)
                .Select(g => new Daily2FAStatsDto
                {
                    Date = g.Key,
                    VerificationCount = g.Count(e => e.Success),
                    FailureCount = g.Count(e => !e.Success)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new TwoFactorAnalyticsDto
            {
                TotalVerifications = twoFactorEvents.Count,
                SuccessfulVerifications = successful,
                FailedVerifications = failed,
                SuccessRate = twoFactorEvents.Count > 0 ? (double)successful / twoFactorEvents.Count * 100 : 100,
                BackupCodeUsageCount = backupCodeUsage,
                DailyStats = dailyStats
            };
        }

        private BackupCodesAnalyticsDto AnalyzeBackupCodesUsage(List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            var backupCodeEvents = events.Where(e => e.EventType.Contains("BackupCode")).ToList();
            var generated = backupCodeEvents.Count(e => e.EventType == "BackupCodesGenerated") * 10; // 10 codes per generation
            var used = backupCodeEvents.Count(e => e.EventType == "BackupCodeUsed");

            var usageHistory = backupCodeEvents
                .Select(e => new BackupCodeUsageDto
                {
                    Date = e.CreatedAt,
                    Action = GetBackupCodeAction(e.EventType),
                    Count = e.EventType == "BackupCodesGenerated" ? 10 : 1
                })
                .OrderBy(u => u.Date)
                .ToList();

            return new BackupCodesAnalyticsDto
            {
                TotalGenerated = generated,
                TotalUsed = used,
                TotalExpired = 0, // Would need to query actual backup codes
                UsageRate = generated > 0 ? (double)used / generated * 100 : 0,
                UsageHistory = usageHistory
            };
        }

        private List<HourlyActivityDto> AnalyzeActivityByHour(List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            var hourlyStats = new List<HourlyActivityDto>();

            for (int hour = 0; hour < 24; hour++)
            {
                var count = events.Count(e => e.CreatedAt.Hour == hour);
                hourlyStats.Add(new HourlyActivityDto
                {
                    Hour = hour,
                    ActivityCount = count
                });
            }

            return hourlyStats;
        }

        private ThreatLevelDto AssessThreatLevel(List<Domain.Entities.Authentication.SecurityAuditLog> events, LoginActivityAnalyticsDto loginActivity)
        {
            var threats = new List<string>();
            var recommendations = new List<string>();
            var threatScore = 0;

            // Analyze threats
            if (loginActivity.FailedLogins > 10)
            {
                threats.Add("High number of failed login attempts");
                threatScore += 30;
                recommendations.Add("Review failed login attempts and consider IP blocking");
            }

            var suspiciousIps = events
                .Where(e => !e.Success)
                .GroupBy(e => e.IpAddress)
                .Where(g => g.Count() >= 5)
                .Count();

            if (suspiciousIps > 0)
            {
                threats.Add($"{suspiciousIps} suspicious IP addresses detected");
                threatScore += 25;
                recommendations.Add("Block suspicious IP addresses");
            }

            // Determine threat level
            string level;
            if (threatScore >= 70) level = "Critical";
            else if (threatScore >= 50) level = "High";
            else if (threatScore >= 30) level = "Medium";
            else if (threatScore >= 10) level = "Low";
            else level = "None";

            if (threats.Count == 0)
            {
                recommendations.Add("Continue monitoring security events");
            }

            return new ThreatLevelDto
            {
                Level = level,
                Score = threatScore,
                Threats = threats,
                Recommendations = recommendations
            };
        }

        private async Task<List<GeographicLoginDto>> AnalyzeGeographicDistributionAsync(List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            var loginEvents = events
                .Where(e => e.EventType.Contains("Login") && !string.IsNullOrEmpty(e.IpAddress))
                .GroupBy(e => e.IpAddress)
                .ToList();

            var geographicLogins = new List<GeographicLoginDto>();

            foreach (var group in loginEvents)
            {
                var ipAddress = group.Key!;
                var (country, city) = await _ipGeolocationService.GetLocationAsync(ipAddress);

                geographicLogins.Add(new GeographicLoginDto
                {
                    IpAddress = ipAddress,
                    Country = country,
                    City = city,
                    LoginCount = group.Count(),
                    LastLogin = group.Max(e => e.CreatedAt)
                });
            }

            return geographicLogins
                .OrderByDescending(g => g.LoginCount)
                .Take(10)
                .ToList();
        }

        private List<DeviceAnalyticsDto> AnalyzeDeviceDistribution(List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            var eventsWithUserAgent = events
                .Where(e => !string.IsNullOrEmpty(e.UserAgent))
                .ToList();

            var deviceGroups = eventsWithUserAgent
                .GroupBy(e => e.UserAgent)
                .Select(g =>
                {
                    var (deviceType, browser) = _userAgentParser.Parse(g.Key!);
                    return new
                    {
                        UserAgent = g.Key!,
                        DeviceType = deviceType,
                        Browser = browser,
                        Count = g.Count()
                    };
                })
                .GroupBy(x => new { x.DeviceType, x.Browser })
                .Select(g => new DeviceAnalyticsDto
                {
                    UserAgent = g.First().UserAgent,
                    DeviceType = g.Key.DeviceType,
                    Browser = g.Key.Browser,
                    UsageCount = g.Sum(x => x.Count)
                })
                .OrderByDescending(d => d.UsageCount)
                .Take(10)
                .ToList();

            return deviceGroups;
        }

        private string GetSecurityLevel(int score)
        {
            return score switch
            {
                >= 80 => "Excellent",
                >= 60 => "High",
                >= 40 => "Medium",
                >= 20 => "Low",
                _ => "Critical"
            };
        }

        private string GetBackupCodeAction(string eventType)
        {
            return eventType switch
            {
                "BackupCodesGenerated" => "generated",
                "BackupCodeUsed" => "used",
                "BackupCodeExpiredAttempt" => "expired",
                _ => "other"
            };
        }
    }
}

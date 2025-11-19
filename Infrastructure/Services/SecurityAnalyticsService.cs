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
    /// Service for security analytics and dashboard data
    /// </summary>
    public class SecurityAnalyticsService : ISecurityAnalyticsService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IBackupCodeRepository _backupCodeRepository;
        private readonly ISecurityAuditLogRepository _auditLogRepository;
        private readonly ILocalizationService _localizer;

        public SecurityAnalyticsService(
            IAdminRepository adminRepository,
            IBackupCodeRepository backupCodeRepository,
            ISecurityAuditLogRepository auditLogRepository,
            ILocalizationService localizer)
        {
            _adminRepository = adminRepository;
            _backupCodeRepository = backupCodeRepository;
            _auditLogRepository = auditLogRepository;
            _localizer = localizer;
        }

        public async Task<SecurityDashboardDto> GetSecurityDashboardAsync(Guid adminId)
        {
            // Verify admin exists and is active
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null || admin.IsDeleted)
            {
                throw new NotFoundException(_localizer["Admin.NotFound"]);
            }

            if (!admin.IsActive)
            {
                throw new BadRequestException(_localizer["Account.Deactivated"] ?? "Account is deactivated");
            }

            // Gather all security data in parallel for performance
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var last7Days = now.AddDays(-7);
            var last30Days = now.AddDays(-30);

            // Get 2FA stats
            var twoFactorStats = await GetTwoFactorStatsAsync(admin, last30Days);

            // Get backup codes stats
            var backupCodesStats = await GetBackupCodesStatsAsync(adminId, now);

            // Get recent security events (last 7 days)
            var recentEvents = await GetRecentSecurityEventsAsync(adminId, last7Days, 20);

            // Get failed login stats
            var failedLoginStats = await GetFailedLoginStatsAsync(adminId, last24Hours, last7Days, last30Days);

            // Calculate security score (0-100)
            var securityScore = CalculateSecurityScore(twoFactorStats, backupCodesStats, failedLoginStats);

            // Get security level based on score
            var securityLevel = GetSecurityLevel(securityScore);

            // Generate recommendations
            var recommendations = GenerateRecommendations(twoFactorStats, backupCodesStats, failedLoginStats);

            return new SecurityDashboardDto
            {
                SecurityScore = securityScore,
                SecurityLevel = securityLevel,
                TwoFactorStats = twoFactorStats,
                BackupCodesStats = backupCodesStats,
                RecentEvents = recentEvents,
                FailedLoginStats = failedLoginStats,
                Recommendations = recommendations,
                LastAuditDate = now
            };
        }

        private async Task<TwoFactorStatsDto> GetTwoFactorStatsAsync(Domain.Entities.Authentication.Admin admin, DateTime last30Days)
        {
            // Get 2FA verification events
            var verificationEvents = await _auditLogRepository.GetByAdminAndEventTypeAsync(
                admin.Id,
                "TwoFactorVerified",
                null,
                null
            );

            var failedEvents = await _auditLogRepository.GetByAdminAndEventTypeAsync(
                admin.Id,
                "TwoFactorVerificationFailed",
                last30Days,
                DateTime.UtcNow
            );

            return new TwoFactorStatsDto
            {
                IsEnabled = admin.IsTwoFactorEnabled,
                EnabledDate = null, // Not tracked yet - could add a separate field for this
                LastVerification = verificationEvents.OrderByDescending(e => e.CreatedAt).FirstOrDefault()?.CreatedAt,
                TotalVerifications = verificationEvents.Count,
                FailedAttemptsLast30Days = failedEvents.Count
            };
        }

        private async Task<BackupCodesStatsDto> GetBackupCodesStatsAsync(Guid adminId, DateTime now)
        {
            var allCodes = await _backupCodeRepository.GetAllByAdminIdAsync(adminId);

            var availableCodes = allCodes.Where(c => !c.IsUsed && c.ExpiresAt > now).ToList();
            var usedCodes = allCodes.Count(c => c.IsUsed);
            var expiredCodes = allCodes.Count(c => c.ExpiresAt <= now);

            DateTime? lastGenerationDate = allCodes.Any() ? allCodes.Max(c => c.CreatedAt) : null;
            DateTime? nextExpiryDate = availableCodes.Any() ? availableCodes.Min(c => c.ExpiresAt) : null;
            int? daysUntilExpiry = nextExpiryDate.HasValue ? (int)(nextExpiryDate.Value - now).TotalDays : null;

            return new BackupCodesStatsDto
            {
                TotalGenerated = allCodes.Count,
                RemainingCodes = availableCodes.Count,
                UsedCodes = usedCodes,
                ExpiredCodes = expiredCodes,
                LastGenerationDate = lastGenerationDate,
                NextExpiryDate = nextExpiryDate,
                DaysUntilExpiry = daysUntilExpiry,
                NeedsRegeneration = availableCodes.Count == 0
            };
        }

        private async Task<List<SecurityEventDto>> GetRecentSecurityEventsAsync(Guid adminId, DateTime since, int limit)
        {
            var events = await _auditLogRepository.GetRecentByAdminAsync(adminId, since, limit);

            return events.Select(e => new SecurityEventDto
            {
                EventType = e.EventType,
                Description = e.EventDescription,
                Timestamp = e.CreatedAt,
                IpAddress = e.IpAddress,
                Success = e.Success,
                Severity = GetEventSeverity(e.EventType, e.Success)
            }).ToList();
        }

        private async Task<FailedLoginStatsDto> GetFailedLoginStatsAsync(
            Guid adminId,
            DateTime last24Hours,
            DateTime last7Days,
            DateTime last30Days)
        {
            // Get failed login events
            var failedLogins24h = await _auditLogRepository.GetByAdminAndEventTypeAsync(
                adminId, "LoginFailed", last24Hours, DateTime.UtcNow);

            var failedLogins7d = await _auditLogRepository.GetByAdminAndEventTypeAsync(
                adminId, "LoginFailed", last7Days, DateTime.UtcNow);

            var failedLogins30d = await _auditLogRepository.GetByAdminAndEventTypeAsync(
                adminId, "LoginFailed", last30Days, DateTime.UtcNow);

            // Get suspicious IPs (3+ failed attempts from same IP)
            var ipGroups = failedLogins7d
                .Where(e => !string.IsNullOrEmpty(e.IpAddress))
                .GroupBy(e => e.IpAddress)
                .Where(g => g.Count() >= 3)
                .Select(g => g.Key!)
                .ToList();

            return new FailedLoginStatsDto
            {
                Last24Hours = failedLogins24h.Count,
                Last7Days = failedLogins7d.Count,
                Last30Days = failedLogins30d.Count,
                MostRecentAttempt = failedLogins30d.OrderByDescending(e => e.CreatedAt).FirstOrDefault()?.CreatedAt,
                SuspiciousActivity = ipGroups.Count > 0 || failedLogins24h.Count >= 5,
                SuspiciousIps = ipGroups
            };
        }

        private int CalculateSecurityScore(
            TwoFactorStatsDto twoFactorStats,
            BackupCodesStatsDto backupCodesStats,
            FailedLoginStatsDto failedLoginStats)
        {
            int score = 0;

            // 2FA enabled: +40 points
            if (twoFactorStats.IsEnabled)
            {
                score += 40;
            }

            // Backup codes generated: +20 points
            if (backupCodesStats.TotalGenerated > 0)
            {
                score += 20;
            }

            // Backup codes available: +15 points
            if (backupCodesStats.RemainingCodes > 0)
            {
                score += 15;
            }

            // No failed logins in 24h: +10 points
            if (failedLoginStats.Last24Hours == 0)
            {
                score += 10;
            }

            // No suspicious activity: +10 points
            if (!failedLoginStats.SuspiciousActivity)
            {
                score += 10;
            }

            // Backup codes not expiring soon: +5 points
            if (backupCodesStats.DaysUntilExpiry > 30 || !backupCodesStats.DaysUntilExpiry.HasValue)
            {
                score += 5;
            }

            // Penalties
            // High failed login attempts: -10 points
            if (failedLoginStats.Last24Hours >= 5)
            {
                score -= 10;
            }

            // Suspicious activity detected: -15 points
            if (failedLoginStats.SuspiciousActivity)
            {
                score -= 15;
            }

            // Backup codes expired: -10 points
            if (backupCodesStats.ExpiredCodes > 0)
            {
                score -= 10;
            }

            // Ensure score is between 0-100
            return Math.Max(0, Math.Min(100, score));
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

        private List<string> GenerateRecommendations(
            TwoFactorStatsDto twoFactorStats,
            BackupCodesStatsDto backupCodesStats,
            FailedLoginStatsDto failedLoginStats)
        {
            var recommendations = new List<string>();

            // 2FA recommendations
            if (!twoFactorStats.IsEnabled)
            {
                recommendations.Add("Enable Two-Factor Authentication for enhanced security");
            }

            // Backup codes recommendations
            if (backupCodesStats.TotalGenerated == 0)
            {
                recommendations.Add("Generate backup codes for account recovery");
            }
            else if (backupCodesStats.NeedsRegeneration)
            {
                recommendations.Add("Regenerate backup codes - all codes are used or expired");
            }
            else if (backupCodesStats.DaysUntilExpiry.HasValue && backupCodesStats.DaysUntilExpiry.Value < 30)
            {
                recommendations.Add($"Backup codes expire in {backupCodesStats.DaysUntilExpiry} days - consider regenerating");
            }
            else if (backupCodesStats.RemainingCodes < 3)
            {
                recommendations.Add($"Only {backupCodesStats.RemainingCodes} backup codes remaining - generate new ones");
            }

            // Failed login recommendations
            if (failedLoginStats.SuspiciousActivity)
            {
                recommendations.Add("Suspicious activity detected - review recent login attempts");
            }

            if (failedLoginStats.Last24Hours >= 3)
            {
                recommendations.Add("Multiple failed login attempts detected - verify your account security");
            }

            // General recommendations
            if (recommendations.Count == 0)
            {
                recommendations.Add("Your account security is excellent - keep it up!");
            }

            return recommendations;
        }

        private string GetEventSeverity(string eventType, bool success)
        {
            if (!success)
            {
                return eventType switch
                {
                    "LoginFailed" => "Warning",
                    "TwoFactorVerificationFailed" => "Warning",
                    "BackupCodeVerificationFailed" => "Warning",
                    "BackupCodeExpiredAttempt" => "Info",
                    _ => "Info"
                };
            }

            return eventType switch
            {
                "2FADisabled" => "Critical",
                "2FAEnabled" => "Info",
                "BackupCodesGenerated" => "Info",
                "BackupCodeUsed" => "Warning",
                "PasswordChanged" => "Info",
                _ => "Info"
            };
        }
    }
}

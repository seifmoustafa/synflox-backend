using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Security;
using Application.Services;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// Service for generating and exporting security reports
    /// </summary>
    public class SecurityReportService : ISecurityReportService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly ISecurityAuditLogRepository _auditLogRepository;
        private readonly ISecurityAnalyticsService _analyticsService;
        private readonly ILocalizationService _localizer;

        public SecurityReportService(
            IAdminRepository adminRepository,
            ISecurityAuditLogRepository auditLogRepository,
            ISecurityAnalyticsService analyticsService,
            ILocalizationService localizer)
        {
            _adminRepository = adminRepository;
            _auditLogRepository = auditLogRepository;
            _analyticsService = analyticsService;
            _localizer = localizer;
        }

        public async Task<SecurityReportDataDto> GenerateReportDataAsync(Guid adminId, SecurityReportRequest request)
        {
            // Verify admin exists
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null || admin.IsDeleted)
            {
                throw new NotFoundException(_localizer["Admin.NotFound"]);
            }

            // Get security dashboard for period
            var dashboard = await _analyticsService.GetSecurityDashboardAsync(adminId);

            // Get all events in period
            var events = await _auditLogRepository.GetRecentByAdminAsync(adminId, request.StartDate, 10000);
            events = events.Where(e => e.CreatedAt >= request.StartDate && e.CreatedAt <= request.EndDate).ToList();

            // Build report metadata
            var metadata = new SecurityReportMetadataDto
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportType = request.ReportType,
                GeneratedAt = DateTime.UtcNow,
                PeriodStart = request.StartDate,
                PeriodEnd = request.EndDate,
                GeneratedBy = adminId.ToString(),
                AdminName = $"{admin.FirstName} {admin.LastName}".Trim()
            };

            // Build executive summary
            var executiveSummary = BuildExecutiveSummary(dashboard, events);

            // Build events summary
            var eventsSummary = BuildEventsSummary(events);

            // Build threat assessment
            var threatAssessment = BuildThreatAssessment(events, dashboard);

            // Build recommendations
            var recommendations = BuildRecommendations(dashboard, events);

            // Include detailed events if requested
            List<SecurityEventDto>? detailedEvents = null;
            if (request.IncludeEventLogs)
            {
                detailedEvents = events.Select(e => new SecurityEventDto
                {
                    EventType = e.EventType,
                    Description = e.EventDescription,
                    Timestamp = e.CreatedAt,
                    IpAddress = e.IpAddress,
                    Success = e.Success,
                    Severity = GetEventSeverity(e.EventType, e.Success)
                }).ToList();
            }

            return new SecurityReportDataDto
            {
                Metadata = metadata,
                ExecutiveSummary = executiveSummary,
                EventsSummary = eventsSummary,
                ThreatAssessment = threatAssessment,
                Recommendations = recommendations,
                DetailedEvents = detailedEvents
            };
        }

        public async Task<SecurityReportExportDto> ExportSecurityReportAsync(Guid adminId, SecurityReportRequest request)
        {
            // Generate report data
            var reportData = await GenerateReportDataAsync(adminId, request);

            // Export based on format
            string fileContent;
            string contentType;
            string fileName;

            var periodStr = $"{request.StartDate:yyyy-MM-dd}_to_{request.EndDate:yyyy-MM-dd}";

            switch (request.Format.ToLower())
            {
                case "pdf":
                    fileContent = GeneratePdfReport(reportData);
                    contentType = "application/pdf";
                    fileName = $"Security_Report_{periodStr}.pdf";
                    break;

                case "excel":
                    fileContent = GenerateExcelReport(reportData);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName = $"Security_Report_{periodStr}.xlsx";
                    break;

                case "json":
                    fileContent = GenerateJsonReport(reportData);
                    contentType = "application/json";
                    fileName = $"Security_Report_{periodStr}.json";
                    break;

                default:
                    throw new BadRequestException($"Invalid export format: {request.Format}");
            }

            return new SecurityReportExportDto
            {
                FileContent = fileContent,
                ContentType = contentType,
                FileName = fileName,
                GeneratedAt = DateTime.UtcNow,
                Period = periodStr,
                TotalEvents = reportData.DetailedEvents?.Count ?? 0
            };
        }

        private ExecutiveSummaryDto BuildExecutiveSummary(SecurityDashboardDto dashboard, List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            var critical = events.Count(e => !e.Success && (e.EventType.Contains("Failed") || e.EventType.Contains("Suspicious")));
            var warning = events.Count(e => e.EventType.Contains("Warning") || e.EventType.Contains("Low"));
            var info = events.Count - critical - warning;

            var keyFindings = new List<string>();
            if (dashboard.SecurityScore >= 80)
                keyFindings.Add("Excellent security posture maintained");
            if (dashboard.FailedLoginStats.SuspiciousActivity)
                keyFindings.Add($"Suspicious activity detected from {dashboard.FailedLoginStats.SuspiciousIps.Count} IP addresses");
            if (dashboard.BackupCodesStats.NeedsRegeneration)
                keyFindings.Add("Backup codes need regeneration");
            if (keyFindings.Count == 0)
                keyFindings.Add("No significant security concerns");

            return new ExecutiveSummaryDto
            {
                SecurityScore = dashboard.SecurityScore,
                SecurityLevel = dashboard.SecurityLevel,
                TotalEvents = events.Count,
                CriticalEvents = critical,
                WarningEvents = warning,
                InfoEvents = info,
                ThreatScore = dashboard.FailedLoginStats.SuspiciousActivity ? 60 : 20,
                KeyFindings = keyFindings
            };
        }

        private SecurityEventsSummaryDto BuildEventsSummary(List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            return new SecurityEventsSummaryDto
            {
                TotalLogins = events.Count(e => e.EventType.Contains("Login")),
                SuccessfulLogins = events.Count(e => e.EventType.Contains("Login") && e.Success),
                FailedLogins = events.Count(e => e.EventType == "LoginFailed"),
                TwoFactorVerifications = events.Count(e => e.EventType == "TwoFactorVerified"),
                BackupCodeUsages = events.Count(e => e.EventType == "BackupCodeUsed"),
                PasswordChanges = events.Count(e => e.EventType == "PasswordChanged"),
                SecuritySettingsChanges = events.Count(e => e.EventType.Contains("2FA") || e.EventType.Contains("BackupCodes")),
                SuspiciousActivities = events.Count(e => e.EventType.Contains("Suspicious") || e.EventType.Contains("Failed"))
            };
        }

        private ThreatAssessmentDto BuildThreatAssessment(List<Domain.Entities.Authentication.SecurityAuditLog> events, SecurityDashboardDto dashboard)
        {
            var threats = new List<ThreatDetailDto>();
            var vulnerabilities = new List<VulnerabilityDto>();

            // Detect threats
            var failedLogins = events.Where(e => e.EventType == "LoginFailed").ToList();
            if (failedLogins.Count >= 5)
            {
                threats.Add(new ThreatDetailDto
                {
                    Type = "Brute Force Attack",
                    Severity = failedLogins.Count >= 10 ? "High" : "Medium",
                    Description = $"{failedLogins.Count} failed login attempts detected",
                    Occurrences = failedLogins.Count,
                    FirstDetected = failedLogins.Min(e => e.CreatedAt),
                    LastDetected = failedLogins.Max(e => e.CreatedAt)
                });
            }

            // Detect vulnerabilities
            if (!dashboard.TwoFactorStats.IsEnabled)
            {
                vulnerabilities.Add(new VulnerabilityDto
                {
                    Category = "Authentication",
                    Description = "Two-Factor Authentication is disabled",
                    Severity = "High",
                    Recommendation = "Enable 2FA immediately to enhance account security"
                });
            }

            if (dashboard.BackupCodesStats.RemainingCodes < 3)
            {
                vulnerabilities.Add(new VulnerabilityDto
                {
                    Category = "Recovery",
                    Description = $"Only {dashboard.BackupCodesStats.RemainingCodes} backup codes remaining",
                    Severity = "Medium",
                    Recommendation = "Generate new backup codes to ensure account recovery options"
                });
            }

            var threatScore = threats.Count * 20 + vulnerabilities.Count * 10;
            var threatLevel = threatScore >= 60 ? "High" : threatScore >= 30 ? "Medium" : "Low";

            return new ThreatAssessmentDto
            {
                OverallThreatLevel = threatLevel,
                ThreatScore = Math.Min(100, threatScore),
                DetectedThreats = threats,
                Vulnerabilities = vulnerabilities
            };
        }

        private List<SecurityRecommendationDto> BuildRecommendations(SecurityDashboardDto dashboard, List<Domain.Entities.Authentication.SecurityAuditLog> events)
        {
            var recommendations = new List<SecurityRecommendationDto>();

            foreach (var rec in dashboard.Recommendations)
            {
                var priority = rec.Contains("immediately") || rec.Contains("Critical") ? "Critical" :
                              rec.Contains("soon") || rec.Contains("consider") ? "High" : "Medium";

                recommendations.Add(new SecurityRecommendationDto
                {
                    Priority = priority,
                    Category = "Security",
                    Title = rec,
                    Description = rec,
                    Action = "Follow the recommendation",
                    Impact = "Improved security posture"
                });
            }

            return recommendations;
        }

        private string GeneratePdfReport(SecurityReportDataDto reportData)
        {
            // Simple text-based PDF (in production, use proper PDF library like iTextSharp)
            var content = new StringBuilder();
            content.AppendLine("SYNFLOX SECURITY REPORT");
            content.AppendLine("======================");
            content.AppendLine();
            content.AppendLine($"Report ID: {reportData.Metadata.ReportId}");
            content.AppendLine($"Generated: {reportData.Metadata.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
            content.AppendLine($"Period: {reportData.Metadata.PeriodStart:yyyy-MM-dd} to {reportData.Metadata.PeriodEnd:yyyy-MM-dd}");
            content.AppendLine($"Admin: {reportData.Metadata.AdminName}");
            content.AppendLine();
            content.AppendLine("EXECUTIVE SUMMARY");
            content.AppendLine("-----------------");
            content.AppendLine($"Security Score: {reportData.ExecutiveSummary.SecurityScore}/100 ({reportData.ExecutiveSummary.SecurityLevel})");
            content.AppendLine($"Total Events: {reportData.ExecutiveSummary.TotalEvents}");
            content.AppendLine($"Critical Events: {reportData.ExecutiveSummary.CriticalEvents}");
            content.AppendLine();
            content.AppendLine("KEY FINDINGS:");
            foreach (var finding in reportData.ExecutiveSummary.KeyFindings)
            {
                content.AppendLine($"- {finding}");
            }
            content.AppendLine();
            content.AppendLine("THREAT ASSESSMENT");
            content.AppendLine("-----------------");
            content.AppendLine($"Overall Threat Level: {reportData.ThreatAssessment.OverallThreatLevel}");
            content.AppendLine($"Threat Score: {reportData.ThreatAssessment.ThreatScore}/100");
            content.AppendLine();
            if (reportData.ThreatAssessment.DetectedThreats.Count > 0)
            {
                content.AppendLine("DETECTED THREATS:");
                foreach (var threat in reportData.ThreatAssessment.DetectedThreats)
                {
                    content.AppendLine($"- [{threat.Severity}] {threat.Type}: {threat.Description}");
                }
            }

            var bytes = Encoding.UTF8.GetBytes(content.ToString());
            return Convert.ToBase64String(bytes);
        }

        private string GenerateExcelReport(SecurityReportDataDto reportData)
        {
            // Mock Excel format (in production, use library like EPPlus or NPOI)
            return GeneratePdfReport(reportData); // For now, same as PDF
        }

        private string GenerateJsonReport(SecurityReportDataDto reportData)
        {
            var json = JsonSerializer.Serialize(reportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }

        private string GetEventSeverity(string eventType, bool success)
        {
            if (!success)
            {
                return eventType switch
                {
                    "LoginFailed" => "Warning",
                    "TwoFactorVerificationFailed" => "Warning",
                    _ => "Info"
                };
            }

            return eventType switch
            {
                "2FADisabled" => "Critical",
                "BackupCodeUsed" => "Warning",
                _ => "Info"
            };
        }
    }
}

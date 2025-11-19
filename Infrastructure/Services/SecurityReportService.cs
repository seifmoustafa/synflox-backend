using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Security;
using Application.Services;
using Domain.Exceptions;
using Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

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
        private readonly IFileHostExportService _fileHostExportService;

        public SecurityReportService(
            IAdminRepository adminRepository,
            ISecurityAuditLogRepository auditLogRepository,
            ISecurityAnalyticsService analyticsService,
            ILocalizationService localizer,
            IFileHostExportService fileHostExportService)
        {
            _adminRepository = adminRepository;
            _auditLogRepository = auditLogRepository;
            _analyticsService = analyticsService;
            _localizer = localizer;
            _fileHostExportService = fileHostExportService;
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

            // Build executive summary (always included, customized by report type)
            var executiveSummary = BuildExecutiveSummary(dashboard, events, request.ReportType);

            // Build sections based on ReportType
            var eventsSummary = BuildEventsSummary(events);
            var threatAssessment = BuildThreatAssessment(events, dashboard);
            var recommendations = BuildRecommendations(dashboard, events);
            List<SecurityEventDto>? detailedEvents = null;

            // Customize content based on ReportType
            switch (request.ReportType)
            {
                case Domain.Enums.ReportType.Summary:
                    // Summary: Only high-level metrics, no detailed events
                    // Keep executive summary, basic recommendations
                    recommendations = recommendations.Where(r => r.Priority == "High" || r.Priority == "Critical").ToList();
                    break;

                case Domain.Enums.ReportType.Detailed:
                    // Detailed: Full report with all sections + detailed events
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
                    break;

                case Domain.Enums.ReportType.Audit:
                    // Audit: Focus on compliance and audit trail
                    // Filter events to show only audit-relevant events
                    var auditEvents = events.Where(e => 
                        e.EventType.Contains("Created") || 
                        e.EventType.Contains("Updated") || 
                        e.EventType.Contains("Deleted") ||
                        e.EventType.Contains("Activated") ||
                        e.EventType.Contains("Deactivated") ||
                        e.EventType.Contains("Generated") ||
                        e.EventType.Contains("Exported")
                    ).ToList();
                    
                    if (request.IncludeEventLogs)
                    {
                        detailedEvents = auditEvents.Select(e => new SecurityEventDto
                        {
                            EventType = e.EventType,
                            Description = e.EventDescription,
                            Timestamp = e.CreatedAt,
                            IpAddress = e.IpAddress,
                            Success = e.Success,
                            Severity = GetEventSeverity(e.EventType, e.Success)
                        }).ToList();
                    }
                    
                    // Focus recommendations on compliance
                    recommendations = recommendations.Where(r => 
                        r.Title.Contains("Audit") || 
                        r.Title.Contains("Logging") ||
                        r.Title.Contains("Compliance") ||
                        r.Priority == "High" ||
                        r.Priority == "Critical"
                    ).ToList();
                    break;

                case Domain.Enums.ReportType.Threat:
                    // Threat: Focus on security threats and failed attempts
                    var threatEvents = events.Where(e => 
                        !e.Success || 
                        e.EventType.Contains("Failed") || 
                        e.EventType.Contains("Locked") ||
                        e.EventType.Contains("Suspended") ||
                        e.EventType.Contains("Blocked")
                    ).ToList();
                    
                    if (request.IncludeEventLogs)
                    {
                        detailedEvents = threatEvents.Select(e => new SecurityEventDto
                        {
                            EventType = e.EventType,
                            Description = e.EventDescription,
                            Timestamp = e.CreatedAt,
                            IpAddress = e.IpAddress,
                            Success = e.Success,
                            Severity = GetEventSeverity(e.EventType, e.Success)
                        }).ToList();
                    }
                    
                    // Focus recommendations on security threats
                    recommendations = recommendations.Where(r => 
                        r.Title.Contains("Security") || 
                        r.Title.Contains("Authentication") ||
                        r.Title.Contains("Two-Factor") ||
                        r.Title.Contains("Password") ||
                        r.Priority == "Critical"
                    ).ToList();
                    
                    // Threat assessment already built - focus on threat-specific events
                    break;
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
            byte[] fileBytes;
            string contentType;
            string fileName;

            var periodStr = $"{request.StartDate:yyyy-MM-dd}_to_{request.EndDate:yyyy-MM-dd}";
            var normalizedFormat = request.Format.ToLower();
            
            // Normalize format aliases
            if (normalizedFormat == "text") normalizedFormat = "txt";
            if (normalizedFormat == "doc") normalizedFormat = "docx";

            switch (normalizedFormat)
            {
                case "pdf":
                    fileBytes = GeneratePdfReport(reportData);
                    contentType = "application/pdf";
                    fileName = $"Security_Report_{periodStr}.pdf";
                    break;

                case "txt":
                    fileBytes = GenerateTextReport(reportData);
                    contentType = "text/plain";
                    fileName = $"Security_Report_{periodStr}.txt";
                    break;

                case "docx":
                    fileBytes = GenerateDocxReport(reportData);
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    fileName = $"Security_Report_{periodStr}.docx";
                    break;

                case "csv":
                    fileBytes = GenerateCsvReport(reportData);
                    contentType = "text/csv";
                    fileName = $"Security_Report_{periodStr}.csv";
                    break;

                case "json":
                    fileBytes = GenerateJsonReport(reportData);
                    contentType = "application/json";
                    fileName = $"Security_Report_{periodStr}.json";
                    break;

                default:
                    throw new BadRequestException($"Unsupported export format: {request.Format}. Supported formats: json, txt, pdf, docx, doc, csv");
            }

            // Save to FileHost and get download URL (auto-deletes after 30 minutes)
            var fileHostResponse = await _fileHostExportService.SaveExportFileAsync(
                fileBytes,
                fileName,
                contentType,
                normalizedFormat
            );

            return new SecurityReportExportDto
            {
                FileContent = fileHostResponse.DownloadUrl, // Changed: Now returns download URL
                ContentType = contentType,
                FileName = fileName,
                GeneratedAt = fileHostResponse.GeneratedAt,
                Period = periodStr,
                TotalEvents = reportData.DetailedEvents?.Count ?? 0
            };
        }

        private ExecutiveSummaryDto BuildExecutiveSummary(SecurityDashboardDto dashboard, List<Domain.Entities.Authentication.SecurityAuditLog> events, Domain.Enums.ReportType reportType)
        {
            var critical = events.Count(e => !e.Success && (e.EventType.Contains("Failed") || e.EventType.Contains("Suspicious")));
            var warning = events.Count(e => e.EventType.Contains("Warning") || e.EventType.Contains("Low"));
            var info = events.Count - critical - warning;

            // Customize key findings based on report type
            var keyFindings = new List<string>();
            
            switch (reportType)
            {
                case Domain.Enums.ReportType.Summary:
                    // Summary: High-level overview
                    keyFindings.Add($"Overall security score: {dashboard.SecurityScore}/100 ({dashboard.SecurityLevel})");
                    if (critical > 0)
                        keyFindings.Add($"{critical} critical security events require immediate attention");
                    if (dashboard.FailedLoginStats.SuspiciousActivity)
                        keyFindings.Add($"Suspicious login attempts from {dashboard.FailedLoginStats.SuspiciousIps.Count} IP addresses");
                    if (keyFindings.Count == 1)
                        keyFindings.Add("No significant security concerns detected");
                    break;

                case Domain.Enums.ReportType.Detailed:
                    // Detailed: Comprehensive findings
                    if (dashboard.SecurityScore >= 80)
                        keyFindings.Add("Excellent security posture maintained across all metrics");
                    else if (dashboard.SecurityScore >= 60)
                        keyFindings.Add("Good security posture with room for improvement");
                    else
                        keyFindings.Add("Security posture requires immediate attention");
                    
                    keyFindings.Add($"Total events analyzed: {events.Count} ({critical} critical, {warning} warnings, {info} informational)");
                    
                    if (dashboard.FailedLoginStats.SuspiciousActivity)
                        keyFindings.Add($"Detected suspicious activity from {dashboard.FailedLoginStats.SuspiciousIps.Count} unique IP addresses");
                    if (dashboard.BackupCodesStats.NeedsRegeneration)
                        keyFindings.Add("Backup codes inventory below threshold - regeneration recommended");
                    // Additional detailed findings covered by comprehensive analysis
                    break;

                case Domain.Enums.ReportType.Audit:
                    // Audit: Compliance-focused findings
                    var auditEvents = events.Where(e => 
                        e.EventType.Contains("Created") || 
                        e.EventType.Contains("Updated") || 
                        e.EventType.Contains("Deleted") ||
                        e.EventType.Contains("Activated") ||
                        e.EventType.Contains("Deactivated")
                    ).Count();
                    
                    keyFindings.Add($"Audit trail contains {auditEvents} administrative actions for compliance review");
                    keyFindings.Add("System changes tracked with full administrator accountability");
                    
                    if (critical > 0)
                        keyFindings.Add($"{critical} failed administrative actions require investigation");
                    
                    var passwordChanges = events.Count(e => e.EventType == "PasswordChanged");
                    if (passwordChanges > 0)
                        keyFindings.Add($"{passwordChanges} password changes recorded during audit period");
                    
                    keyFindings.Add("All administrative actions logged with IP address and timestamp for compliance");
                    break;

                case Domain.Enums.ReportType.Threat:
                    // Threat: Security threat-focused findings
                    var failedLogins = events.Count(e => e.EventType == "LoginFailed");
                    var blockedAttempts = events.Count(e => !e.Success);
                    
                    if (blockedAttempts == 0)
                    {
                        keyFindings.Add("No security threats detected during monitoring period");
                        keyFindings.Add("All authentication attempts successful - system security intact");
                    }
                    else
                    {
                        keyFindings.Add($"ALERT: {blockedAttempts} potential security threats detected");
                        if (failedLogins > 0)
                            keyFindings.Add($"{failedLogins} failed login attempts - possible brute force attack");
                    }
                    
                    if (dashboard.FailedLoginStats.SuspiciousActivity)
                        keyFindings.Add($"CRITICAL: Suspicious activity from {dashboard.FailedLoginStats.SuspiciousIps.Count} IP addresses");
                    
                    var twoFactorCount = events.Count(e => e.EventType == "TwoFactorVerified");
                    if (twoFactorCount == 0 && events.Count(e => e.EventType == "LoginSuccessful") > 0)
                        keyFindings.Add("VULNERABILITY: No two-factor authentication usage detected");
                    
                    var threatLevel = blockedAttempts > 10 ? "HIGH" : blockedAttempts > 5 ? "MEDIUM" : "LOW";
                    keyFindings.Add($"Current threat level: {threatLevel}");
                    break;
            }

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

        /// <summary>
        /// Generate STUNNING, PROFESSIONAL, CREATIVE PDF with CHARTS and SYNFLOX Branding
        /// Premium security report with data visualization and modern design
        /// </summary>
        private byte[] GeneratePdfReport(SecurityReportDataDto reportData)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    
                    // STUNNING BRANDED HEADER with Icon and Security Badge
                    page.Header().Height(180).Column(header =>
                    {
                        header.Item().Height(180).Layers(layers =>
                        {
                            // Purple gradient base
                            layers.Layer().Background(Colors.Purple.Darken2);
                            layers.PrimaryLayer().Padding(20).Column(content =>
                            {
                                // Brand + Security Badge Row
                                content.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(brand =>
                                    {
                                        brand.Item().Text("🛡️ SYNFLOX")
                                            .FontSize(32).Bold().FontColor(Colors.White);
                                        brand.Item().PaddingTop(2).Text("Security Intelligence Report")
                                            .FontSize(11).FontColor(Colors.Grey.Lighten3);
                                    });
                                    row.ConstantItem(100).AlignRight().Column(badge =>
                                    {
                                        badge.Item().Background(Colors.Red.Medium).Padding(8).AlignCenter()
                                            .Text("🔒 CONFIDENTIAL").FontSize(8).Bold().FontColor(Colors.White);
                                    });
                                });
                                
                                content.Item().PaddingTop(12).AlignCenter().Column(title =>
                                {
                                    title.Item().Text("COMPREHENSIVE SECURITY ANALYSIS")
                                        .FontSize(20).Bold().FontColor(Colors.White);
                                    title.Item().PaddingTop(4).Text($"Period: {reportData.Metadata.PeriodStart:MMM dd, yyyy} - {reportData.Metadata.PeriodEnd:MMM dd, yyyy}")
                                        .FontSize(11).FontColor(Colors.Grey.Lighten4);
                                    title.Item().PaddingTop(2).Text($"Generated: {reportData.Metadata.GeneratedAt:MMM dd, yyyy HH:mm UTC}")
                                        .FontSize(9).FontColor(Colors.Grey.Lighten3);
                                });
                            });
                        });
                    });

                    // PREMIUM CONTENT with CHARTS
                    page.Content().Padding(25).Column(column =>
                    {
                        // REPORT INFO CARD - Modern Design
                        column.Item().Border(2).BorderColor(Colors.Purple.Lighten2)
                            .Background(Colors.Purple.Lighten5).Padding(18).Column(infoCard =>
                        {
                            infoCard.Item().Text("📋 Report Details").FontSize(13).SemiBold().FontColor(Colors.Purple.Darken2);
                            infoCard.Item().PaddingTop(8).PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Purple.Lighten3);
                            
                            infoCard.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("👤 Administrator").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    col.Item().PaddingTop(3).Text(reportData.Metadata.AdminName).FontSize(10).Bold().FontColor(Colors.Purple.Darken3);
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("📊 Report Type").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    col.Item().PaddingTop(3).Text(reportData.Metadata.ReportType.ToString()).FontSize(10).Bold().FontColor(Colors.Purple.Darken3);
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("📅 Period").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    col.Item().PaddingTop(3).Text($"{reportData.Metadata.PeriodStart:MMM dd} - {reportData.Metadata.PeriodEnd:MMM dd, yyyy}").FontSize(10).Bold().FontColor(Colors.Purple.Darken3);
                                });
                            });
                        });

                        // EXECUTIVE SUMMARY with VISUAL CHART
                        column.Item().PaddingTop(20).Border(3).BorderColor(GetScoreColor(reportData.ExecutiveSummary.SecurityScore))
                            .Background(Colors.White).Column(summary =>
                        {
                            // Header with Score Badge
                            summary.Item().Background(GetScoreColor(reportData.ExecutiveSummary.SecurityScore)).Padding(15).Row(row =>
                            {
                                row.RelativeItem().Column(left =>
                                {
                                    left.Item().Text("📊 EXECUTIVE SUMMARY").FontSize(18).Bold().FontColor(Colors.White);
                                    left.Item().PaddingTop(3).Text("Overall Security Posture").FontSize(11).FontColor(Colors.Grey.Lighten4);
                                });
                                row.ConstantItem(120).AlignRight().AlignMiddle().Column(scoreBox =>
                                {
                                    scoreBox.Item().Background(Colors.White).Padding(12).AlignCenter().Column(score =>
                                    {
                                        score.Item().Text($"{reportData.ExecutiveSummary.SecurityScore}").FontSize(32).Bold().FontColor(GetScoreColor(reportData.ExecutiveSummary.SecurityScore));
                                        score.Item().Text("/100").FontSize(12).FontColor(Colors.Grey.Darken2);
                                        score.Item().PaddingTop(2).Text(reportData.ExecutiveSummary.SecurityLevel).FontSize(10).Bold().FontColor(GetScoreColor(reportData.ExecutiveSummary.SecurityScore));
                                    });
                                });
                            });
                            
                            // Events Breakdown with BAR CHART
                            summary.Item().Padding(20).Column(events =>
                            {
                                events.Item().Text("📈 Event Distribution (Visual Chart)").FontSize(14).SemiBold().FontColor(Colors.Purple.Darken2);
                                
                                var totalEvents = reportData.ExecutiveSummary.TotalEvents > 0 ? reportData.ExecutiveSummary.TotalEvents : 1;
                                
                                // Critical Events Bar
                                events.Item().PaddingTop(12).Row(row =>
                                {
                                    var criticalCount = reportData.ExecutiveSummary.CriticalEvents;
                                    var criticalPct = (criticalCount * 100.0 / totalEvents);
                                    row.ConstantItem(120).Text($"🔴 Critical: {criticalCount}").FontSize(10).SemiBold();
                                    row.RelativeItem().PaddingRight(10).Row(barRow =>
                                    {
                                        if (criticalPct > 0)
                                            barRow.RelativeItem((float)criticalPct).Background(Colors.Red.Medium).Height(20).AlignMiddle()
                                                .Padding(4).AlignRight().Text($"{criticalPct:F1}%").FontSize(8).Bold().FontColor(Colors.White);
                                        if (criticalPct < 100)
                                            barRow.RelativeItem((float)(100 - criticalPct)).Background(Colors.Grey.Lighten3).Height(20);
                                    });
                                });
                                
                                // Warning Events Bar
                                events.Item().PaddingTop(8).Row(row =>
                                {
                                    var warningCount = reportData.ExecutiveSummary.WarningEvents;
                                    var warningPct = (warningCount * 100.0 / totalEvents);
                                    row.ConstantItem(120).Text($"🟠 Warning: {warningCount}").FontSize(10).SemiBold();
                                    row.RelativeItem().PaddingRight(10).Row(barRow =>
                                    {
                                        if (warningPct > 0)
                                            barRow.RelativeItem((float)warningPct).Background(Colors.Orange.Medium).Height(20).AlignMiddle()
                                                .Padding(4).AlignRight().Text($"{warningPct:F1}%").FontSize(8).Bold().FontColor(Colors.White);
                                        if (warningPct < 100)
                                            barRow.RelativeItem((float)(100 - warningPct)).Background(Colors.Grey.Lighten3).Height(20);
                                    });
                                });
                                
                                // Info Events Bar
                                events.Item().PaddingTop(8).Row(row =>
                                {
                                    var infoCount = reportData.ExecutiveSummary.InfoEvents;
                                    var infoPct = (infoCount * 100.0 / totalEvents);
                                    row.ConstantItem(120).Text($"🔵 Info: {infoCount}").FontSize(10).SemiBold();
                                    row.RelativeItem().PaddingRight(10).Row(barRow =>
                                    {
                                        if (infoPct > 0)
                                            barRow.RelativeItem((float)infoPct).Background(Colors.Blue.Medium).Height(20).AlignMiddle()
                                                .Padding(4).AlignRight().Text($"{infoPct:F1}%").FontSize(8).Bold().FontColor(Colors.White);
                                        if (infoPct < 100)
                                            barRow.RelativeItem((float)(100 - infoPct)).Background(Colors.Grey.Lighten3).Height(20);
                                    });
                                });
                                
                                events.Item().PaddingTop(10).Text($"Total Events Analyzed: {reportData.ExecutiveSummary.TotalEvents}")
                                    .FontSize(10).Italic().FontColor(Colors.Grey.Darken1);
                            });
                        });

                        // Key Findings
                        if (reportData.ExecutiveSummary.KeyFindings.Count > 0)
                        {
                            column.Item().PaddingTop(15).Column(findings =>
                            {
                                findings.Item().Text("KEY FINDINGS").FontSize(14).SemiBold();
                                foreach (var finding in reportData.ExecutiveSummary.KeyFindings)
                                {
                                    findings.Item().PaddingTop(5).Text($"• {finding}").FontSize(10);
                                }
                            });
                        }

                        // Threat Assessment
                        column.Item().PaddingTop(15).Column(threat =>
                        {
                            threat.Item().Text("THREAT ASSESSMENT").FontSize(14).SemiBold();
                            threat.Item().PaddingTop(5).Text($"Overall Level: {reportData.ThreatAssessment.OverallThreatLevel} (Score: {reportData.ThreatAssessment.ThreatScore}/100)").FontSize(11);
                            
                            if (reportData.ThreatAssessment.DetectedThreats.Count > 0)
                            {
                                threat.Item().PaddingTop(10).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(80);
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(80);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Severity").FontSize(10).SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Threat Type").FontSize(10).SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Occurrences").FontSize(10).SemiBold();
                                    });

                                    foreach (var t in reportData.ThreatAssessment.DetectedThreats)
                                    {
                                        table.Cell().Padding(5).Text(t.Severity).FontSize(9);
                                        table.Cell().Padding(5).Text(t.Type).FontSize(9);
                                        table.Cell().Padding(5).Text(t.Occurrences.ToString()).FontSize(9);
                                    }
                                });
                            }
                            else
                            {
                                threat.Item().PaddingTop(5).Text("No threats detected.").FontSize(10).Italic();
                            }
                        });

                        // Recommendations
                        if (reportData.Recommendations != null && reportData.Recommendations.Count > 0)
                        {
                            column.Item().PaddingTop(15).Column(rec =>
                            {
                                rec.Item().Text("SECURITY RECOMMENDATIONS").FontSize(14).SemiBold();
                                foreach (var recommendation in reportData.Recommendations)
                                {
                                    rec.Item().PaddingTop(8).Background(Colors.Blue.Lighten4).Padding(8).Column(r =>
                                    {
                                        r.Item().Text($"[{recommendation.Priority}] {recommendation.Title}").FontSize(11).SemiBold();
                                        r.Item().PaddingTop(3).Text(recommendation.Description).FontSize(9);
                                    });
                                }
                            });
                        }
                    });

                    // PREMIUM FOOTER with BRANDING
                    page.Footer().Height(70).Column(footer =>
                    {
                        footer.Item().LineHorizontal(2).LineColor(Colors.Purple.Medium);
                        footer.Item().PaddingTop(12).Background(Colors.Grey.Lighten4).Padding(15).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("SYNFLOX Central Licensing System")
                                    .FontSize(12).Bold().FontColor(Colors.Purple.Darken2);
                                left.Item().Text("© 2025 SYNFLOX - All Rights Reserved | Confidential Security Report")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                            row.ConstantItem(140).AlignRight().AlignMiddle().Column(right =>
                            {
                                right.Item().AlignRight().Text($"Generated: {DateTime.UtcNow:MMM dd, yyyy}")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                                right.Item().AlignRight().Text("Page 1 of 1")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private QuestPDF.Infrastructure.Color GetScoreColor(int score)
        {
            if (score >= 80) return Colors.Green.Medium;
            if (score >= 60) return Colors.Orange.Medium;
            return Colors.Red.Medium;
        }

        /// <summary>
        /// Generate CSV report (comma-separated values)
        /// Production-ready for immediate use
        /// </summary>
        private byte[] GenerateCsvReport(SecurityReportDataDto reportData)
        {
            var content = new StringBuilder();
            content.AppendLine("SYNFLOX Security Report - Event Log");
            content.AppendLine();
            content.AppendLine($"Generated,{reportData.Metadata.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Period,{reportData.Metadata.PeriodStart:yyyy-MM-dd} to {reportData.Metadata.PeriodEnd:yyyy-MM-dd}");
            content.AppendLine($"Administrator,{reportData.Metadata.AdminName}");
            content.AppendLine();
            content.AppendLine("Event Type,Severity,Timestamp,IP Address,Description");
            
            if (reportData.DetailedEvents != null)
            {
                foreach (var evt in reportData.DetailedEvents)
                {
                    var severity = evt.Severity ?? "Info";
                    var description = evt.Description?.Replace(",", ";") ?? "";
                    content.AppendLine($"{evt.EventType},{severity},{evt.Timestamp:yyyy-MM-dd HH:mm:ss},{evt.IpAddress},{description}");
                }
            }

            return Encoding.UTF8.GetBytes(content.ToString());
        }

        /// <summary>
        /// Generate plain text report
        /// Production-ready clean format
        /// </summary>
        private byte[] GenerateTextReport(SecurityReportDataDto reportData)
        {
            var content = new StringBuilder();
            content.AppendLine("SYNFLOX SECURITY REPORT");
            content.AppendLine("=======================");
            content.AppendLine();
            content.AppendLine($"Generated:      {reportData.Metadata.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
            content.AppendLine($"Period:         {reportData.Metadata.PeriodStart:yyyy-MM-dd} to {reportData.Metadata.PeriodEnd:yyyy-MM-dd}");
            content.AppendLine($"Administrator:  {reportData.Metadata.AdminName}");
            content.AppendLine($"Report Type:    {reportData.Metadata.ReportType}");
            content.AppendLine();
            content.AppendLine("EXECUTIVE SUMMARY");
            content.AppendLine("-----------------");
            content.AppendLine($"Security Score:   {reportData.ExecutiveSummary.SecurityScore}/100 ({reportData.ExecutiveSummary.SecurityLevel})");
            content.AppendLine($"Total Events:     {reportData.ExecutiveSummary.TotalEvents}");
            content.AppendLine($"Critical Events:  {reportData.ExecutiveSummary.CriticalEvents}");
            content.AppendLine($"Warning Events:   {reportData.ExecutiveSummary.WarningEvents}");
            content.AppendLine($"Info Events:      {reportData.ExecutiveSummary.InfoEvents}");
            content.AppendLine();
            content.AppendLine("KEY FINDINGS:");
            foreach (var finding in reportData.ExecutiveSummary.KeyFindings)
            {
                content.AppendLine($"  • {finding}");
            }
            content.AppendLine();
            content.AppendLine("THREAT ASSESSMENT");
            content.AppendLine("-----------------");
            content.AppendLine($"Overall Threat Level: {reportData.ThreatAssessment.OverallThreatLevel}");
            content.AppendLine($"Threat Score:         {reportData.ThreatAssessment.ThreatScore}/100");
            content.AppendLine();
            if (reportData.ThreatAssessment.DetectedThreats.Count > 0)
            {
                content.AppendLine("DETECTED THREATS:");
                foreach (var threat in reportData.ThreatAssessment.DetectedThreats)
                {
                    content.AppendLine($"  • [{threat.Severity}] {threat.Type}");
                    content.AppendLine($"    {threat.Description}");
                    content.AppendLine($"    Occurrences: {threat.Occurrences}");
                    content.AppendLine();
                }
            }
            else
            {
                content.AppendLine("No threats detected during this period.");
            }
            content.AppendLine();
            content.AppendLine("SECURITY RECOMMENDATIONS");
            content.AppendLine("------------------------");
            if (reportData.Recommendations != null && reportData.Recommendations.Count > 0)
            {
                foreach (var recommendation in reportData.Recommendations)
                {
                    content.AppendLine($"  • [{recommendation.Priority}] {recommendation.Title}");
                    content.AppendLine($"    {recommendation.Description}");
                    content.AppendLine();
                }
            }
            else
            {
                content.AppendLine("No specific recommendations at this time.");
            }
            content.AppendLine();
            content.AppendLine("=======================================");
            content.AppendLine("SYNFLOX Central Licensing System © 2025");
            content.AppendLine("=======================================");

            return Encoding.UTF8.GetBytes(content.ToString());
        }

        /// <summary>
        /// Generate REAL DOCX report using DocumentFormat.OpenXml
        /// Production-ready Word document
        /// </summary>
        private byte[] GenerateDocxReport(SecurityReportDataDto reportData)
        {
            using var stream = new MemoryStream();
            
            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Title
                AddDocxParagraph(body, "SYNFLOX SECURITY REPORT", true, "32", "7B68EE");
                AddDocxParagraph(body, $"Period: {reportData.Metadata.PeriodStart:yyyy-MM-dd} to {reportData.Metadata.PeriodEnd:yyyy-MM-dd}", false, "18", "808080");
                body.AppendChild(new Paragraph());

                // Report Info
                AddDocxParagraph(body, $"Generated: {reportData.Metadata.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC", false, "16");
                AddDocxParagraph(body, $"Period: {reportData.Metadata.PeriodStart:yyyy-MM-dd} to {reportData.Metadata.PeriodEnd:yyyy-MM-dd}", false, "16");
                AddDocxParagraph(body, $"Administrator: {reportData.Metadata.AdminName}", false, "16");
                AddDocxParagraph(body, $"Report Type: {reportData.Metadata.ReportType}", false, "16");
                body.AppendChild(new Paragraph());

                // Executive Summary
                AddDocxParagraph(body, "EXECUTIVE SUMMARY", true, "24", "7B68EE");
                AddDocxParagraph(body, $"Security Score: {reportData.ExecutiveSummary.SecurityScore}/100 ({reportData.ExecutiveSummary.SecurityLevel})", false, "18");
                AddDocxParagraph(body, $"Total Events: {reportData.ExecutiveSummary.TotalEvents}", false, "18");
                AddDocxParagraph(body, $"Critical: {reportData.ExecutiveSummary.CriticalEvents} | Warning: {reportData.ExecutiveSummary.WarningEvents} | Info: {reportData.ExecutiveSummary.InfoEvents}", false, "18");
                body.AppendChild(new Paragraph());

                // Key Findings
                if (reportData.ExecutiveSummary.KeyFindings.Count > 0)
                {
                    AddDocxParagraph(body, "Key Findings:", true, "20");
                    foreach (var finding in reportData.ExecutiveSummary.KeyFindings)
                    {
                        AddDocxParagraph(body, $"• {finding}", false, "18");
                    }
                    body.AppendChild(new Paragraph());
                }

                // Threat Assessment
                AddDocxParagraph(body, "THREAT ASSESSMENT", true, "24", "DC143C");
                AddDocxParagraph(body, $"Overall Level: {reportData.ThreatAssessment.OverallThreatLevel} (Score: {reportData.ThreatAssessment.ThreatScore}/100)", false, "18");
                
                if (reportData.ThreatAssessment.DetectedThreats.Count > 0)
                {
                    body.AppendChild(new Paragraph());
                    var table = CreateDocxTable(body);
                    AddDocxTableRow(table, new[] { "Severity", "Type", "Occurrences" }, true);
                    foreach (var threat in reportData.ThreatAssessment.DetectedThreats)
                    {
                        AddDocxTableRow(table, new[] { threat.Severity, threat.Type, threat.Occurrences.ToString() }, false);
                    }
                }
                body.AppendChild(new Paragraph());

                // Recommendations
                if (reportData.Recommendations != null && reportData.Recommendations.Count > 0)
                {
                    AddDocxParagraph(body, "SECURITY RECOMMENDATIONS", true, "24", "228B22");
                    foreach (var rec in reportData.Recommendations)
                    {
                        AddDocxParagraph(body, $"[{rec.Priority}] {rec.Title}", true, "18");
                        AddDocxParagraph(body, rec.Description, false, "16");
                        body.AppendChild(new Paragraph());
                    }
                }

                // Footer
                body.AppendChild(new Paragraph());
                AddDocxParagraph(body, "SYNFLOX Central Licensing System © 2025", false, "16", "808080");

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private void AddDocxParagraph(Body body, string text, bool bold = false, string fontSize = "20", string color = "000000")
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));
            var props = run.AppendChild(new RunProperties());
            if (bold) props.AppendChild(new Bold());
            props.AppendChild(new FontSize() { Val = fontSize });
            props.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = color });
        }

        private Table CreateDocxTable(Body body)
        {
            var table = body.AppendChild(new Table());
            var tableProps = table.AppendChild(new TableProperties());
            tableProps.AppendChild(new TableBorders(
                new TopBorder() { Val = BorderValues.Single, Size = 6 },
                new BottomBorder() { Val = BorderValues.Single, Size = 6 },
                new LeftBorder() { Val = BorderValues.Single, Size = 6 },
                new RightBorder() { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 6 },
                new InsideVerticalBorder() { Val = BorderValues.Single, Size = 6 }
            ));
            return table;
        }

        private void AddDocxTableRow(Table table, string[] cells, bool isHeader)
        {
            var row = table.AppendChild(new TableRow());
            foreach (var cellText in cells)
            {
                var cell = row.AppendChild(new TableCell());
                var para = cell.AppendChild(new Paragraph());
                var run = para.AppendChild(new Run());
                run.AppendChild(new Text(cellText));
                var props = run.AppendChild(new RunProperties());
                if (isHeader)
                {
                    props.AppendChild(new Bold());
                    var cellProps = cell.AppendChild(new TableCellProperties());
                    cellProps.AppendChild(new Shading() { Val = ShadingPatternValues.Clear, Fill = "D3D3D3" });
                }
                props.AppendChild(new FontSize() { Val = "18" });
            }
        }

        private byte[] GenerateJsonReport(SecurityReportDataDto reportData)
        {
            var json = JsonSerializer.Serialize(reportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return Encoding.UTF8.GetBytes(json);
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

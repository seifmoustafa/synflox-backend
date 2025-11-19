using System;
using System.Collections.Generic;

namespace Application.DTOs.Security
{
    /// <summary>
    /// Security report request parameters
    /// </summary>
    public class SecurityReportRequest
    {
        /// <summary>
        /// Report type: "summary", "detailed", "audit", "threat"
        /// </summary>
        public required string ReportType { get; set; }

        /// <summary>
        /// Start date for report period
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date for report period
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Export format: "pdf", "excel", "json"
        /// </summary>
        public required string Format { get; set; }

        /// <summary>
        /// Include detailed event logs
        /// </summary>
        public bool IncludeEventLogs { get; set; }

        /// <summary>
        /// Include charts and graphs
        /// </summary>
        public bool IncludeCharts { get; set; }
    }

    /// <summary>
    /// Security report export response
    /// </summary>
    public class SecurityReportExportDto
    {
        /// <summary>
        /// Base64-encoded file content
        /// </summary>
        public required string FileContent { get; set; }

        /// <summary>
        /// MIME type
        /// </summary>
        public required string ContentType { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// Report generation timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Report period
        /// </summary>
        public required string Period { get; set; }

        /// <summary>
        /// Total events included
        /// </summary>
        public int TotalEvents { get; set; }
    }

    /// <summary>
    /// Comprehensive security report data
    /// </summary>
    public class SecurityReportDataDto
    {
        /// <summary>
        /// Report metadata
        /// </summary>
        public required SecurityReportMetadataDto Metadata { get; set; }

        /// <summary>
        /// Executive summary
        /// </summary>
        public required ExecutiveSummaryDto ExecutiveSummary { get; set; }

        /// <summary>
        /// Security events summary
        /// </summary>
        public required SecurityEventsSummaryDto EventsSummary { get; set; }

        /// <summary>
        /// Threat assessment
        /// </summary>
        public required ThreatAssessmentDto ThreatAssessment { get; set; }

        /// <summary>
        /// Recommendations
        /// </summary>
        public required List<SecurityRecommendationDto> Recommendations { get; set; }

        /// <summary>
        /// Detailed event logs (optional)
        /// </summary>
        public List<SecurityEventDto>? DetailedEvents { get; set; }
    }

    /// <summary>
    /// Report metadata
    /// </summary>
    public class SecurityReportMetadataDto
    {
        public required string ReportId { get; set; }
        public required string ReportType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public required string GeneratedBy { get; set; }
        public required string AdminName { get; set; }
    }

    /// <summary>
    /// Executive summary
    /// </summary>
    public class ExecutiveSummaryDto
    {
        public int SecurityScore { get; set; }
        public required string SecurityLevel { get; set; }
        public int TotalEvents { get; set; }
        public int CriticalEvents { get; set; }
        public int WarningEvents { get; set; }
        public int InfoEvents { get; set; }
        public double ThreatScore { get; set; }
        public required List<string> KeyFindings { get; set; }
    }

    /// <summary>
    /// Security events summary
    /// </summary>
    public class SecurityEventsSummaryDto
    {
        public int TotalLogins { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLogins { get; set; }
        public int TwoFactorVerifications { get; set; }
        public int BackupCodeUsages { get; set; }
        public int PasswordChanges { get; set; }
        public int SecuritySettingsChanges { get; set; }
        public int SuspiciousActivities { get; set; }
    }

    /// <summary>
    /// Threat assessment
    /// </summary>
    public class ThreatAssessmentDto
    {
        public required string OverallThreatLevel { get; set; }
        public int ThreatScore { get; set; }
        public required List<ThreatDetailDto> DetectedThreats { get; set; }
        public required List<VulnerabilityDto> Vulnerabilities { get; set; }
    }

    /// <summary>
    /// Individual threat detail
    /// </summary>
    public class ThreatDetailDto
    {
        public required string Type { get; set; }
        public required string Severity { get; set; }
        public required string Description { get; set; }
        public int Occurrences { get; set; }
        public DateTime FirstDetected { get; set; }
        public DateTime LastDetected { get; set; }
    }

    /// <summary>
    /// Security vulnerability
    /// </summary>
    public class VulnerabilityDto
    {
        public required string Category { get; set; }
        public required string Description { get; set; }
        public required string Severity { get; set; }
        public required string Recommendation { get; set; }
    }

    /// <summary>
    /// Security recommendation
    /// </summary>
    public class SecurityRecommendationDto
    {
        public required string Priority { get; set; } // "Critical", "High", "Medium", "Low"
        public required string Category { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Action { get; set; }
        public required string Impact { get; set; }
    }
}

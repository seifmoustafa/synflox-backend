namespace Application.DTOs.Security
{
    /// <summary>
    /// Custom branding options for security reports
    /// Allows customization of report appearance per deployment
    /// </summary>
    public class SecurityReportBrandingDto
    {
        /// <summary>
        /// Company name to display in report header (default: SYNFLOX)
        /// </summary>
        public string CompanyName { get; set; } = "SYNFLOX";

        /// <summary>
        /// Report title (default: Security Intelligence Report)
        /// </summary>
        public string ReportTitle { get; set; } = "Security Intelligence Report";

        /// <summary>
        /// Primary brand color in hex format (default: #7B68EE - Purple)
        /// </summary>
        public string PrimaryColor { get; set; } = "#7B68EE";

        /// <summary>
        /// Company logo URL or base64 image (optional)
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Footer text (default: SYNFLOX Central Licensing System © 2025)
        /// </summary>
        public string FooterText { get; set; } = "SYNFLOX Central Licensing System © 2025";

        /// <summary>
        /// Whether to include company logo in reports (default: false)
        /// </summary>
        public bool IncludeLogo { get; set; } = false;

        /// <summary>
        /// Custom watermark text (optional)
        /// </summary>
        public string? WatermarkText { get; set; }

        /// <summary>
        /// Whether to add watermark to PDF reports (default: false)
        /// </summary>
        public bool IncludeWatermark { get; set; } = false;
    }
}

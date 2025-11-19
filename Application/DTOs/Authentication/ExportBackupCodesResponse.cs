using System;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Response for exporting backup codes in various formats
    /// Returns base64-encoded content for file download
    /// </summary>
    public class ExportBackupCodesResponse
    {
        /// <summary>
        /// Base64-encoded file content ready for download
        /// </summary>
        public required string FileContent { get; set; }

        /// <summary>
        /// MIME type for the file (application/pdf, text/plain, application/json)
        /// </summary>
        public required string ContentType { get; set; }

        /// <summary>
        /// Suggested filename for download
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// Number of unused codes included in the export
        /// </summary>
        public int UnusedCodesCount { get; set; }

        /// <summary>
        /// Export format (PDF, Text, JSON)
        /// </summary>
        public required string Format { get; set; }

        /// <summary>
        /// Timestamp when the export was generated
        /// </summary>
        public DateTime ExportedAt { get; set; }

        /// <summary>
        /// Message to display to user
        /// </summary>
        public string? Message { get; set; }
    }
}

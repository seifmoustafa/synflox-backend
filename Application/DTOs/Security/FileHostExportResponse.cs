using System;

namespace Application.DTOs.Security
{
    /// <summary>
    /// Response for FileHost-based exports with download URL
    /// </summary>
    public class FileHostExportResponse
    {
        /// <summary>
        /// Unique file identifier
        /// </summary>
        public required string FileId { get; set; }

        /// <summary>
        /// Direct download URL
        /// </summary>
        public required string DownloadUrl { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// MIME type
        /// </summary>
        public required string ContentType { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// When the file was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// When the file will be auto-deleted (30 minutes)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Minutes until file expires
        /// </summary>
        public int MinutesUntilExpiry => Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalMinutes);
    }
}

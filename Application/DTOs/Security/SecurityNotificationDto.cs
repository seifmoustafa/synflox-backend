using System;

namespace Application.DTOs.Security
{
    /// <summary>
    /// Real-time security notification data
    /// </summary>
    public class SecurityNotificationDto
    {
        /// <summary>
        /// Notification ID
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Notification type (e.g., "2FA_ENABLED", "BACKUP_CODE_USED")
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Notification title
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Notification message
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Severity: "info", "warning", "error", "success"
        /// </summary>
        public required string Severity { get; set; }

        /// <summary>
        /// Timestamp when notification was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// IP address (if applicable)
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Whether notification requires action
        /// </summary>
        public bool RequiresAction { get; set; }

        /// <summary>
        /// Action URL (if applicable)
        /// </summary>
        public string? ActionUrl { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Authentication
{
    /// <summary>
    /// Tracks all security-related events for audit and forensics
    /// Used to detect suspicious activity and security breaches
    /// </summary>
    public class SecurityAuditLog : BaseEntity<Guid>
    {
        /// <summary>
        /// Admin ID who performed the action (nullable for failed login attempts)
        /// </summary>
        public Guid? AdminId { get; set; }

        /// <summary>
        /// Type of security event
        /// Examples: "BackupCodesGenerated", "BackupCodeUsed", "BackupCodeVerificationFailed", 
        /// "LoginSuccess", "LoginFailed", "2FAEnabled", "2FADisabled", "PasswordChanged"
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string EventType { get; set; }

        /// <summary>
        /// Detailed description of the event
        /// </summary>
        [StringLength(500)]
        public string? EventDescription { get; set; }

        /// <summary>
        /// Username or email attempted (for failed logins)
        /// </summary>
        [StringLength(100)]
        public string? Username { get; set; }

        /// <summary>
        /// IP address from which action was performed
        /// </summary>
        [Required]
        [StringLength(45)] // IPv6 max length
        public required string IpAddress { get; set; }

        /// <summary>
        /// User agent (browser/app information)
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if action failed
        /// </summary>
        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp of the event
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata in JSON format
        /// Examples: { "remainingCodes": 5, "batchId": "..." }
        /// </summary>
        [StringLength(2000)]
        public string? Metadata { get; set; }

        // Navigation property
        public Admin? Admin { get; set; }
    }
}

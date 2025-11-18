using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Authentication
{
    /// <summary>
    /// Represents a password reset token for OTP-based password recovery
    /// </summary>
    public class PasswordResetToken : BaseEntity<Guid>
    {

        /// <summary>
        /// Admin ID requesting password reset
        /// </summary>
        [Required]
        public Guid AdminId { get; set; }

        /// <summary>
        /// SHA256 hash of the 6-digit OTP code
        /// Never store plain text OTP for security
        /// </summary>
        [Required]
        [StringLength(64)]
        public required string TokenHash { get; set; }

        /// <summary>
        /// Expiration time (15 minutes from creation)
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Whether this token has been used for password reset
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Timestamp when token was used
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IP address from which reset was requested
        /// Used for security logging
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }

        /// <summary>
        /// Number of failed verification attempts
        /// Max 5 attempts allowed
        /// </summary>
        public int FailedAttempts { get; set; } = 0;

        // Navigation property
        public Admin Admin { get; set; } = null!;

        /// <summary>
        /// Check if token is expired
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        /// <summary>
        /// Check if token is valid (not used and not expired)
        /// </summary>
        public bool IsValid => !IsUsed && !IsExpired;
    }
}

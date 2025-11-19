using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Authentication
{
    /// <summary>
    /// Represents a backup code for 2FA recovery
    /// Generated in sets of 10 codes when 2FA is enabled
    /// Each code is single-use and hashed for security
    /// </summary>
    public class BackupCode : BaseEntity<Guid>
    {
        /// <summary>
        /// Admin ID who owns this backup code
        /// </summary>
        [Required]
        public Guid AdminId { get; set; }

        /// <summary>
        /// SHA256 hash of the 8-character backup code
        /// Never store plain text codes for security
        /// Format: XXXXXXXX (uppercase alphanumeric)
        /// </summary>
        [Required]
        [StringLength(64)]
        public required string CodeHash { get; set; }

        /// <summary>
        /// Whether this backup code has been used
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Timestamp when code was used
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Expiration timestamp (90 days from creation)
        /// Codes expire after 90 days for security
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Generation batch ID - links codes generated together
        /// Useful for invalidating entire sets and rate limiting
        /// </summary>
        [Required]
        public Guid BatchId { get; set; }

        // Navigation property
        public Admin Admin { get; set; } = null!;

        /// <summary>
        /// Check if code is available for use
        /// Code must not be used AND not expired
        /// </summary>
        public bool IsAvailable => !IsUsed && DateTime.UtcNow < ExpiresAt;

        /// <summary>
        /// Check if code is expired
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}

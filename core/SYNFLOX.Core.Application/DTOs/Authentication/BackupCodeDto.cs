using System;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// DTO for backup code information
    /// </summary>
    public class BackupCodeDto
    {
        /// <summary>
        /// Backup code ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Plain text backup code (only shown once during generation)
        /// Format: XXXXXXXX (8 characters, uppercase alphanumeric)
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Whether this code has been used
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// When the code was used
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// When the code was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Licensing
{
    /// <summary>
    /// Represents a company (tenant) in the SYNFLOX licensing system.
    /// </summary>
    public class Company : AuditEntity<Guid>
    {
        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

        /// <summary>
        /// Indicates whether the company subscription is active.
        /// When false, the subscription is suspended regardless of expiry date.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The date when the subscription expires.
        /// Null means the subscription never expires.
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        [StringLength(200)]
        public string? ContactEmail { get; set; }

        [StringLength(50)]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// Encrypted license key for offline systems.
        /// Stored encrypted in the database.
        /// </summary>
        [StringLength(1000)]
        public string? LicenseKey { get; set; }
    }
}


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

        [StringLength(200)]
        public string? ContactEmail { get; set; }

        [StringLength(50)]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// Indicates whether the company is active or deactivated
        /// </summary>
        public bool IsActive { get; set; } = true;

        // License keys are now managed per subscription, not per company
        // See Subscription.OfflineLicenseKey for offline license key management
    }
}


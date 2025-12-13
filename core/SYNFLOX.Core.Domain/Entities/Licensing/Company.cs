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

        /// <summary>
        /// Timezone identifier for the company (IANA format).
        /// Used for time-based access window calculations.
        /// Example: "Africa/Cairo", "America/New_York"
        /// </summary>
        [StringLength(100)]
        public string? TimezoneId { get; set; }

        // License keys are now managed per subscription, not per company
        // See Subscription.OfflineLicenseKey for offline license key management

        // Navigation properties
        
        /// <summary>
        /// The company's admin account for device management.
        /// One admin per company.
        /// </summary>
        public virtual CompanyAdmin? Admin { get; set; }
    }
}


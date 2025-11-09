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
        /// The date when the subscription expires.
        /// Null means the subscription never expires.
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Indicates if the subscription has expired.
        /// This flag is automatically set by the background service when ExpiryDate passes.
        /// </summary>
        public bool IsExpired { get; set; }

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

        /// <summary>
        /// The subscription plan ID for this company.
        /// </summary>
        public Guid? SubscriptionPlanId { get; set; }

        /// <summary>
        /// Navigation property to the subscription plan.
        /// </summary>
        public SubscriptionPlan? SubscriptionPlan { get; set; }

        /// <summary>
        /// Indicates if this is a trial subscription.
        /// </summary>
        public bool IsTrial { get; set; }

        /// <summary>
        /// The date when the trial period ends.
        /// </summary>
        public DateTime? TrialEndDate { get; set; }

        /// <summary>
        /// Navigation property to the custom fields for this company.
        /// </summary>
        public ICollection<CompanyCustomField> CustomFields { get; set; } = new List<CompanyCustomField>();
    }
}

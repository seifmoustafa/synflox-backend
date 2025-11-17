using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Authentication
{
    public class Admin : AuditEntity<Guid>
    {
        // ===== Basic Information =====
        [Required]
        [StringLength(100)]
        public required string Username { get; set; }

        [Required]
        [StringLength(100)]
        public required string Password { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public Gender? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        // ===== Profile Picture =====
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        // ===== Professional Information =====
        [StringLength(100)]
        public string? JobTitle { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        // ===== Preferences =====
        [StringLength(10)]
        public string? PreferredLanguage { get; set; } = "en"; // "en" or "ar"

        [StringLength(50)]
        public string? Timezone { get; set; }

        [StringLength(20)]
        public string? ThemePreference { get; set; } = "light"; // "light" or "dark"

        [StringLength(20)]
        public string? DateFormat { get; set; } = "MM/dd/yyyy";

        [StringLength(20)]
        public string? TimeFormat { get; set; } = "12h"; // "12h" or "24h"

        // ===== Social & Contact =====
        [StringLength(200)]
        public string? LinkedInUrl { get; set; }

        [StringLength(200)]
        public string? TwitterUrl { get; set; }

        [StringLength(100)]
        public string? BackupEmail { get; set; }

        // ===== Security & Activity =====
        public DateTime? LastLoginAt { get; set; }

        public DateTime? LastPasswordChangeAt { get; set; }

        public int LoginCount { get; set; } = 0;

        public bool IsTwoFactorEnabled { get; set; } = false;

        [StringLength(500)]
        public string? TwoFactorSecret { get; set; }

        // ===== Notification Preferences =====
        public bool EmailNotificationsEnabled { get; set; } = true;

        public bool PushNotificationsEnabled { get; set; } = true;

        public bool CompanyExpiryNotifications { get; set; } = true;

        public bool SubscriptionExpiryNotifications { get; set; } = true;

        public bool SystemAlertsNotifications { get; set; } = true;

        // ===== Admin Type & Status =====
        public Guid AdminTypeId { get; set; }
        public AdminType AdminType { get; set; } = null!;
    }
}

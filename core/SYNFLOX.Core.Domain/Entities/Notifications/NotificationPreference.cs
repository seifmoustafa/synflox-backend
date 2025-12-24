using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Notifications;

/// <summary>
/// User preferences for notification delivery channels and types.
/// </summary>
public class NotificationPreference : BaseEntity<Guid>
{
    /// <summary>
    /// User ID (SuperAdmin or CompanyAdmin)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of user: 'SuperAdmin' or 'CompanyAdmin'
    /// </summary>
    [Required]
    [StringLength(20)]
    public string UserType { get; set; } = "SuperAdmin";

    // ========== Channel Preferences ==========

    /// <summary>
    /// Enable in-app notifications
    /// </summary>
    public bool InAppEnabled { get; set; } = true;

    /// <summary>
    /// Enable email notifications
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// Enable push notifications
    /// </summary>
    public bool PushEnabled { get; set; } = true;

    /// <summary>
    /// Enable SMS notifications (future)
    /// </summary>
    public bool SmsEnabled { get; set; } = false;

    // ========== Type Preferences ==========

    /// <summary>
    /// Subscription-related notifications (expiring, renewed, etc.)
    /// </summary>
    public bool SubscriptionNotifications { get; set; } = true;

    /// <summary>
    /// Security notifications (login, password change, etc.)
    /// </summary>
    public bool SecurityNotifications { get; set; } = true;

    /// <summary>
    /// System notifications (maintenance, updates)
    /// </summary>
    public bool SystemNotifications { get; set; } = true;

    /// <summary>
    /// Promotional notifications (offers, news)
    /// </summary>
    public bool PromotionalNotifications { get; set; } = true;

    /// <summary>
    /// Device-related notifications (limit reached, binding)
    /// </summary>
    public bool DeviceNotifications { get; set; } = true;

    // ========== Timing Preferences ==========

    /// <summary>
    /// Enable quiet hours (no push notifications during this time)
    /// </summary>
    public bool QuietHoursEnabled { get; set; } = false;

    /// <summary>
    /// Quiet hours start time (e.g., "22:00")
    /// </summary>
    [StringLength(5)]
    public string? QuietHoursStart { get; set; }

    /// <summary>
    /// Quiet hours end time (e.g., "08:00")
    /// </summary>
    [StringLength(5)]
    public string? QuietHoursEnd { get; set; }

    /// <summary>
    /// User timezone for quiet hours calculation
    /// </summary>
    [StringLength(50)]
    public string? Timezone { get; set; }

    // ========== Email Preferences ==========

    /// <summary>
    /// Email digest frequency: 'instant', 'daily', 'weekly', 'never'
    /// </summary>
    [StringLength(10)]
    public string EmailDigestFrequency { get; set; } = "instant";

    /// <summary>
    /// When preferences were last updated
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

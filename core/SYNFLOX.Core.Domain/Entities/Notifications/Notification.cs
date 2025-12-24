using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Notifications;

/// <summary>
/// Represents a notification sent to a user (SuperAdmin or CompanyAdmin).
/// Supports multi-channel delivery: in-app, email, push.
/// </summary>
public class Notification : BaseEntity<Guid>
{
    /// <summary>
    /// The user ID this notification is for.
    /// Can be a SuperAdmin ID or CompanyAdmin ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of user: 'SuperAdmin' or 'CompanyAdmin'
    /// </summary>
    [Required]
    [StringLength(20)]
    public string UserType { get; set; } = "SuperAdmin";

    /// <summary>
    /// Notification type for filtering and preferences.
    /// Examples: subscription_expiring, security_alert, promotional
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Notification category for grouping.
    /// Examples: subscription, security, system, marketing
    /// </summary>
    [StringLength(30)]
    public string? Category { get; set; }

    /// <summary>
    /// Notification title (short summary)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full notification message
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON payload with additional data
    /// Can include links, IDs, metadata
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Priority level: low, normal, high, urgent
    /// </summary>
    [StringLength(10)]
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Optional icon name for display
    /// </summary>
    [StringLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Optional action URL for click handling
    /// </summary>
    [StringLength(500)]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Whether notification has been read
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// When the notification was read
    /// </summary>
    public DateTime? ReadAtUtc { get; set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the notification expires (for auto-cleanup)
    /// </summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Whether email was sent for this notification
    /// </summary>
    public bool EmailSent { get; set; } = false;

    /// <summary>
    /// Whether push notification was sent
    /// </summary>
    public bool PushSent { get; set; } = false;

    // Optional references for context
    public Guid? CompanyId { get; set; }
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Check if notification is expired
    /// </summary>
    public bool IsExpired => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value < DateTime.UtcNow;
}

using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Notifications;

/// <summary>
/// Represents an in-system notification.
/// </summary>
public class Notification : BaseEntity<Guid>
{
    /// <summary>
    /// The company this notification belongs to.
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Navigation property to the company.
    /// </summary>
    public Domain.Entities.Licensing.Company Company { get; set; } = null!;

    /// <summary>
    /// The type of notification.
    /// </summary>
    [Required]
    public NotificationType Type { get; set; }

    /// <summary>
    /// Notification title.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    /// <summary>
    /// Notification message.
    /// </summary>
    [Required]
    [StringLength(1000)]
    public required string Message { get; set; }

    /// <summary>
    /// Whether the notification has been read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Timestamp when the notification was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Timestamp when the notification was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


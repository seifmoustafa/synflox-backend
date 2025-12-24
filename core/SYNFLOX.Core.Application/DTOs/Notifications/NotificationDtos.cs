using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Notifications;

/// <summary>
/// DTO for displaying a notification
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
    public string Priority { get; set; } = "normal";
    public string? Icon { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string TimeAgo { get; set; } = string.Empty; // "2 hours ago"
}

/// <summary>
/// DTO for creating a notification
/// </summary>
public class CreateNotificationDto
{
    public Guid UserId { get; set; }
    public string UserType { get; set; } = "SuperAdmin";
    public string Type { get; set; } = string.Empty;
    public string? Category { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? Data { get; set; }
    public string Priority { get; set; } = "normal";
    public string? Icon { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? SubscriptionId { get; set; }
    
    /// <summary>
    /// Whether to send email for this notification
    /// </summary>
    public bool SendEmail { get; set; } = false;
    
    /// <summary>
    /// Whether to send push notification
    /// </summary>
    public bool SendPush { get; set; } = false;
}

/// <summary>
/// DTO for paginated notification list
/// </summary>
public class NotificationListDto
{
    public List<NotificationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>
/// DTO for notification preferences
/// </summary>
public class NotificationPreferenceDto
{
    // Channel preferences
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    
    // Type preferences
    public bool SubscriptionNotifications { get; set; } = true;
    public bool SecurityNotifications { get; set; } = true;
    public bool SystemNotifications { get; set; } = true;
    public bool PromotionalNotifications { get; set; } = true;
    public bool DeviceNotifications { get; set; } = true;
    
    // Timing
    public bool QuietHoursEnabled { get; set; } = false;
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public string? Timezone { get; set; }
    
    // Email
    public string EmailDigestFrequency { get; set; } = "instant";
}

/// <summary>
/// DTO for registering push subscription
/// </summary>
public class RegisterPushSubscriptionDto
{
    public required string Endpoint { get; set; }
    public required string Platform { get; set; }
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }
    public string? DeviceFingerprint { get; set; }
    
    // Web Push specific
    public string? P256dhKey { get; set; }
    public string? AuthKey { get; set; }
}

/// <summary>
/// DTO for real-time notification via SignalR
/// </summary>
public class RealTimeNotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public string? Icon { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int NewUnreadCount { get; set; }
}

/// <summary>
/// DTO for bulk notification creation
/// </summary>
public class BulkNotificationDto
{
    public List<Guid> UserIds { get; set; } = new();
    public string UserType { get; set; } = "SuperAdmin";
    public required string Type { get; set; }
    public string? Category { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? Data { get; set; }
    public string Priority { get; set; } = "normal";
    public string? Icon { get; set; }
    public string? ActionUrl { get; set; }
    public bool SendEmail { get; set; } = false;
    public bool SendPush { get; set; } = false;
}

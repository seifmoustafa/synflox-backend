using System;
using System.Collections.Generic;

namespace Application.DTOs.Notification;

/// <summary>
/// DTO for displaying a single notification
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public string? Icon { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? Data { get; set; }
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
/// DTO for creating a single notification (internal use)
/// </summary>
public class CreateNotificationDto
{
    public Guid UserId { get; set; }
    public string UserType { get; set; } = "CompanyAdmin";
    public string Type { get; set; } = "system_announcement";
    public string? Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
    public string Priority { get; set; } = "normal";
    public string? Icon { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? SubscriptionId { get; set; }
}

/// <summary>
/// Request DTO for sending messages to companies (from Admin)
/// </summary>
public class SendMessageRequestDto
{
    /// <summary>
    /// List of encrypted company IDs to send to
    /// </summary>
    public List<string> CompanyIds { get; set; } = new();

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message body
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether to send in-app notification
    /// </summary>
    public bool SendNotification { get; set; } = true;

    /// <summary>
    /// Whether to send email notification
    /// </summary>
    public bool SendEmail { get; set; } = false;

    /// <summary>
    /// Priority level (low, normal, high, urgent)
    /// </summary>
    public string Priority { get; set; } = "normal";
}

/// <summary>
/// Result DTO for bulk message sending
/// </summary>
public class SendMessageResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalCompanies { get; set; }
    public int NotificationsSent { get; set; }
    public int EmailsSent { get; set; }
    public int CompaniesWithoutAdmin { get; set; }
    public int CompaniesWithoutEmail { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<CompanySendResultDto> CompanyResults { get; set; } = new();
}

/// <summary>
/// Result for a single company in bulk send
/// </summary>
public class CompanySendResultDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool NotificationSent { get; set; }
    public bool EmailSent { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// DTO for recent notifications response (dropdown)
/// </summary>
public class RecentNotificationsDto
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
}

/// <summary>
/// DTO for notification preferences
/// </summary>
public class NotificationPreferenceDto
{
    public Guid Id { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public bool QuietHoursEnabled { get; set; } = false;
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
}

/// <summary>
/// DTO for registering push subscription (Web Push / FCM / APNS).
/// For mobile apps (Flutter/iOS/Android), use Endpoint field for FCM/APNS token.
/// P256dh and Auth are only for Web Push.
/// </summary>
public class RegisterPushSubscriptionDto
{
    /// <summary>
    /// Push endpoint or device token. For Web Push, this is the full endpoint URL.
    /// For FCM/APNS (mobile), this is the device token.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Web Push p256dh key (Web Push only)
    /// </summary>
    public string? P256dh { get; set; }

    /// <summary>
    /// Web Push auth key (Web Push only)
    /// </summary>
    public string? Auth { get; set; }

    /// <summary>
    /// Platform type: web, ios, android, flutter, desktop
    /// </summary>
    public string Platform { get; set; } = "web";

    /// <summary>
    /// Optional device name for display
    /// </summary>
    public string? DeviceName { get; set; }
}

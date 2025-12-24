using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Notifications;
using Application.Services;
using Domain.Entities.Notifications;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Implementation of INotificationService.
/// Handles in-app notifications, preferences, and push subscriptions.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly INotificationPreferenceRepository _preferenceRepo;
    private readonly IPushSubscriptionRepository _pushRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepo,
        INotificationPreferenceRepository preferenceRepo,
        IPushSubscriptionRepository pushRepo,
        IUnitOfWork unitOfWork,
        ILogger<NotificationService> logger)
    {
        _notificationRepo = notificationRepo;
        _preferenceRepo = preferenceRepo;
        _pushRepo = pushRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    #region Notification CRUD

    /// <inheritdoc/>
    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        // Check user preferences before creating notification
        var preferences = await _preferenceRepo.GetByUserIdAsync(dto.UserId, dto.UserType);
        
        // If user has disabled in-app notifications for this category, skip
        if (preferences != null && !ShouldSendNotification(preferences, dto.Type))
        {
            _logger.LogDebug("Skipped notification for user {UserId} - disabled by preferences", dto.UserId);
            return new NotificationDto
            {
                Id = Guid.Empty,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type
            };
        }

        // Check quiet hours if enabled
        if (preferences?.QuietHoursEnabled == true && IsQuietHours(preferences))
        {
            _logger.LogDebug("Skipped notification for user {UserId} - quiet hours active", dto.UserId);
            // Still create the notification, but don't send push/email
            dto.SendPush = false;
            dto.SendEmail = false;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            UserType = dto.UserType,
            Type = dto.Type,
            Category = dto.Category ?? GetCategoryFromType(dto.Type),
            Title = dto.Title,
            Message = dto.Message,
            Data = dto.Data,
            Priority = dto.Priority,
            Icon = dto.Icon ?? GetIconFromType(dto.Type),
            ActionUrl = dto.ActionUrl,
            ExpiresAtUtc = dto.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(30),
            CompanyId = dto.CompanyId,
            SubscriptionId = dto.SubscriptionId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _notificationRepo.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
            notification.Id, dto.UserId);

        var result = MapToDto(notification);

        // Send real-time notification via SignalR if available
        await SendRealTimeNotificationAsync(dto.UserId, dto.UserType, result);

        // Queue email if requested and preferences allow
        if (dto.SendEmail && (preferences?.EmailEnabled ?? true))
        {
            // Email is handled by OutboxEventProcessor - the notification is already created
            // Background job will process pending email notifications
            _logger.LogDebug("Email notification queued for user {UserId}", dto.UserId);
        }

        // Queue push if requested and preferences allow
        if (dto.SendPush && (preferences?.PushEnabled ?? true))
        {
            // Push notifications would be sent via a push service
            // For now, log that it should be sent
            _logger.LogDebug("Push notification queued for user {UserId}", dto.UserId);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<int> CreateBulkAsync(BulkNotificationDto dto)
    {
        var notifications = dto.UserIds.Select(userId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserType = dto.UserType,
            Type = dto.Type,
            Category = dto.Category ?? GetCategoryFromType(dto.Type),
            Title = dto.Title,
            Message = dto.Message,
            Data = dto.Data,
            Priority = dto.Priority,
            Icon = dto.Icon ?? GetIconFromType(dto.Type),
            ActionUrl = dto.ActionUrl,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();

        await _notificationRepo.AddRangeAsync(notifications);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created {Count} bulk notifications", notifications.Count);

        return notifications.Count;
    }

    /// <inheritdoc/>
    public async Task<NotificationListDto> GetByUserIdAsync(
        Guid userId,
        string userType,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false)
    {
        var notifications = await _notificationRepo.GetByUserIdAsync(
            userId, userType, page, pageSize, unreadOnly);
        
        var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId, userType);
        var totalCount = await _notificationRepo.Count(); // Could optimize with user-specific count

        return new NotificationListDto
        {
            Items = notifications.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Page = page,
            PageSize = pageSize,
            HasMore = notifications.Count() == pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<List<NotificationDto>> GetRecentAsync(Guid userId, string userType, int count = 5)
    {
        var notifications = await _notificationRepo.GetRecentAsync(userId, userType, count);
        return notifications.Select(MapToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(Guid userId, string userType)
    {
        return await _notificationRepo.GetUnreadCountAsync(userId, userType);
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        var result = await _notificationRepo.MarkAsReadAsync(notificationId);
        if (result)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<int> MarkAllAsReadAsync(Guid userId, string userType)
    {
        var count = await _notificationRepo.MarkAllAsReadAsync(userId, userType);
        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return count;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid notificationId)
    {
        await _notificationRepo.DeleteAsync(notificationId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Preferences

    /// <inheritdoc/>
    public async Task<NotificationPreferenceDto> GetPreferencesAsync(Guid userId, string userType)
    {
        var preference = await _preferenceRepo.GetOrCreateByUserIdAsync(userId, userType);
        await _unitOfWork.SaveChangesAsync(); // Save if created new

        return MapPreferenceToDto(preference);
    }

    /// <inheritdoc/>
    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid userId,
        string userType,
        NotificationPreferenceDto dto)
    {
        var preference = await _preferenceRepo.GetOrCreateByUserIdAsync(userId, userType);

        // Update fields
        preference.InAppEnabled = dto.InAppEnabled;
        preference.EmailEnabled = dto.EmailEnabled;
        preference.PushEnabled = dto.PushEnabled;
        preference.SmsEnabled = dto.SmsEnabled;
        preference.SubscriptionNotifications = dto.SubscriptionNotifications;
        preference.SecurityNotifications = dto.SecurityNotifications;
        preference.SystemNotifications = dto.SystemNotifications;
        preference.PromotionalNotifications = dto.PromotionalNotifications;
        preference.DeviceNotifications = dto.DeviceNotifications;
        preference.QuietHoursEnabled = dto.QuietHoursEnabled;
        preference.QuietHoursStart = dto.QuietHoursStart;
        preference.QuietHoursEnd = dto.QuietHoursEnd;
        preference.Timezone = dto.Timezone;
        preference.EmailDigestFrequency = dto.EmailDigestFrequency;
        preference.UpdatedAtUtc = DateTime.UtcNow;

        await _preferenceRepo.UpdateAsync(preference);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

        return MapPreferenceToDto(preference);
    }

    #endregion

    #region Push Subscriptions

    /// <inheritdoc/>
    public async Task<bool> RegisterPushSubscriptionAsync(
        Guid userId,
        string userType,
        RegisterPushSubscriptionDto dto)
    {
        // Check if endpoint already exists
        var existing = await _pushRepo.GetByEndpointAsync(dto.Endpoint);
        if (existing != null)
        {
            // Update existing subscription
            existing.UserId = userId;
            existing.UserType = userType;
            existing.Platform = dto.Platform;
            existing.DeviceName = dto.DeviceName;
            existing.AppVersion = dto.AppVersion;
            existing.TokenRefreshedAtUtc = DateTime.UtcNow;
            existing.IsActive = true;
            existing.FailedAttempts = 0;
            await _pushRepo.UpdateAsync(existing);
        }
        else
        {
            // Create new subscription
            var subscription = new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserType = userType,
                Endpoint = dto.Endpoint,
                Platform = dto.Platform,
                DeviceName = dto.DeviceName,
                AppVersion = dto.AppVersion,
                DeviceFingerprint = dto.DeviceFingerprint,
                P256dhKey = dto.P256dhKey,
                AuthKey = dto.AuthKey,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _pushRepo.AddAsync(subscription);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Registered push subscription for user {UserId} on {Platform}", 
            userId, dto.Platform);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UnregisterPushSubscriptionAsync(string endpoint)
    {
        var result = await _pushRepo.DeleteByEndpointAsync(endpoint);
        if (result)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return result;
    }

    #endregion

    #region Convenience Methods

    /// <inheritdoc/>
    public async Task SendSubscriptionExpiryWarningAsync(
        Guid companyAdminId,
        Guid subscriptionId,
        string companyName,
        string planName,
        int daysUntilExpiry)
    {
        var type = daysUntilExpiry switch
        {
            1 => "subscription_expiring_1d",
            7 => "subscription_expiring_7d",
            _ => "subscription_expiring_30d"
        };

        var urgency = daysUntilExpiry switch
        {
            1 => "urgent",
            7 => "high",
            _ => "normal"
        };

        await CreateAsync(new CreateNotificationDto
        {
            UserId = companyAdminId,
            UserType = "CompanyAdmin",
            Type = type,
            Title = $"Subscription Expiring in {daysUntilExpiry} Day(s)",
            Message = $"Your {planName} subscription for {companyName} will expire in {daysUntilExpiry} day(s). Please renew to avoid service interruption.",
            Priority = urgency,
            SubscriptionId = subscriptionId,
            ActionUrl = "/subscriptions",
            SendEmail = true,
            SendPush = daysUntilExpiry <= 7
        });
    }

    /// <inheritdoc/>
    public async Task SendSecurityAlertAsync(
        Guid userId,
        string userType,
        string alertType,
        string message,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        var data = new
        {
            alertType,
            ipAddress,
            deviceInfo,
            timestamp = DateTime.UtcNow
        };

        await CreateAsync(new CreateNotificationDto
        {
            UserId = userId,
            UserType = userType,
            Type = "security_alert",
            Category = "security",
            Title = "Security Alert",
            Message = message,
            Priority = "high",
            Data = System.Text.Json.JsonSerializer.Serialize(data),
            SendEmail = true,
            SendPush = true
        });
    }

    /// <inheritdoc/>
    public async Task SendDeviceLimitReachedAsync(
        Guid companyAdminId,
        Guid subscriptionId,
        string planName,
        int currentCount,
        int maxCount)
    {
        await CreateAsync(new CreateNotificationDto
        {
            UserId = companyAdminId,
            UserType = "CompanyAdmin",
            Type = "device_limit_reached",
            Title = "Device Limit Reached",
            Message = $"You have reached the maximum device limit ({currentCount}/{maxCount}) for your {planName} subscription. Consider upgrading your plan or removing unused devices.",
            Priority = "high",
            SubscriptionId = subscriptionId,
            ActionUrl = "/devices",
            SendEmail = true
        });
    }

    /// <inheritdoc/>
    public async Task<int> SendPromotionalAsync(
        string userType,
        string title,
        string message,
        string? actionUrl = null)
    {
        // This would need to get all user IDs of the specified type
        // For now, returning 0 - implementation depends on user repository
        _logger.LogWarning("SendPromotionalAsync not fully implemented - need user list");
        return 0;
    }

    #endregion

    #region Private Helpers

    private NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Category = n.Category ?? "",
        Title = n.Title,
        Message = n.Message,
        Data = n.Data,
        Priority = n.Priority,
        Icon = n.Icon,
        ActionUrl = n.ActionUrl,
        IsRead = n.IsRead,
        ReadAtUtc = n.ReadAtUtc,
        CreatedAtUtc = n.CreatedAtUtc,
        TimeAgo = GetTimeAgo(n.CreatedAtUtc)
    };

    private NotificationPreferenceDto MapPreferenceToDto(NotificationPreference p) => new()
    {
        InAppEnabled = p.InAppEnabled,
        EmailEnabled = p.EmailEnabled,
        PushEnabled = p.PushEnabled,
        SmsEnabled = p.SmsEnabled,
        SubscriptionNotifications = p.SubscriptionNotifications,
        SecurityNotifications = p.SecurityNotifications,
        SystemNotifications = p.SystemNotifications,
        PromotionalNotifications = p.PromotionalNotifications,
        DeviceNotifications = p.DeviceNotifications,
        QuietHoursEnabled = p.QuietHoursEnabled,
        QuietHoursStart = p.QuietHoursStart,
        QuietHoursEnd = p.QuietHoursEnd,
        Timezone = p.Timezone,
        EmailDigestFrequency = p.EmailDigestFrequency
    };

    private static string GetCategoryFromType(string type) => type switch
    {
        var t when t.StartsWith("subscription") => "subscription",
        var t when t.StartsWith("security") => "security",
        var t when t.StartsWith("device") => "device",
        var t when t.StartsWith("promotional") => "marketing",
        var t when t.StartsWith("system") => "system",
        _ => "general"
    };

    private static string GetIconFromType(string type) => type switch
    {
        var t when t.Contains("expir") => "clock",
        var t when t.Contains("security") => "shield",
        var t when t.Contains("device") => "monitor",
        var t when t.Contains("subscription") => "credit-card",
        var t when t.Contains("promotional") => "gift",
        _ => "bell"
    };

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)}w ago";
        return dateTime.ToString("MMM dd");
    }

    /// <summary>
    /// Check if notification should be sent based on user preferences
    /// </summary>
    private static bool ShouldSendNotification(NotificationPreference preferences, string notificationType)
    {
        // Check if in-app notifications are globally disabled
        if (!preferences.InAppEnabled)
            return false;

        // Check category-specific preferences
        var category = GetCategoryFromType(notificationType);
        return category switch
        {
            "subscription" => preferences.SubscriptionNotifications,
            "security" => preferences.SecurityNotifications,
            "system" => preferences.SystemNotifications,
            "marketing" => preferences.PromotionalNotifications,
            "device" => preferences.DeviceNotifications,
            _ => true // Allow general/unknown categories by default
        };
    }

    /// <summary>
    /// Check if current time is within user's quiet hours
    /// </summary>
    private static bool IsQuietHours(NotificationPreference preferences)
    {
        if (!preferences.QuietHoursEnabled)
            return false;

        if (string.IsNullOrEmpty(preferences.QuietHoursStart) || 
            string.IsNullOrEmpty(preferences.QuietHoursEnd))
            return false;

        try
        {
            var now = DateTime.UtcNow;
            
            // Adjust for user's timezone if specified
            if (!string.IsNullOrEmpty(preferences.Timezone))
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(preferences.Timezone);
                    now = TimeZoneInfo.ConvertTimeFromUtc(now, tz);
                }
                catch
                {
                    // Use UTC if timezone is invalid
                }
            }

            var currentTime = now.TimeOfDay;
            var startTime = TimeSpan.Parse(preferences.QuietHoursStart);
            var endTime = TimeSpan.Parse(preferences.QuietHoursEnd);

            // Handle overnight quiet hours (e.g., 22:00 to 07:00)
            if (startTime > endTime)
            {
                return currentTime >= startTime || currentTime < endTime;
            }
            else
            {
                return currentTime >= startTime && currentTime < endTime;
            }
        }
        catch
        {
            return false; // On any error, allow notifications
        }
    }

    /// <summary>
    /// Send real-time notification via SignalR (no-op if hub service not available)
    /// </summary>
    private Task SendRealTimeNotificationAsync(Guid userId, string userType, NotificationDto notification)
    {
        // SignalR real-time is handled at the API layer via INotificationHubService
        // This service creates the notification, the controller/hub handles real-time
        // Log for debugging purposes
        _logger.LogDebug("Notification {NotificationId} ready for real-time delivery to user {UserId}", 
            notification.Id, userId);
        return Task.CompletedTask;
    }

    #endregion
}

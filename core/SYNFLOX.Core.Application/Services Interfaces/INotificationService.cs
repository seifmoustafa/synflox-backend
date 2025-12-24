using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Notifications;

namespace Application.Services;

/// <summary>
/// Service interface for managing notifications across all channels.
/// Handles in-app notifications, email, push, and preferences.
/// </summary>
public interface INotificationService
{
    // ==================== Notification CRUD ====================

    /// <summary>
    /// Create a notification and optionally send via other channels
    /// </summary>
    Task<NotificationDto> CreateAsync(CreateNotificationDto dto);

    /// <summary>
    /// Create notifications for multiple users
    /// </summary>
    Task<int> CreateBulkAsync(BulkNotificationDto dto);

    /// <summary>
    /// Get paginated notifications for a user
    /// </summary>
    Task<NotificationListDto> GetByUserIdAsync(
        Guid userId, 
        string userType,
        int page = 1, 
        int pageSize = 20,
        bool unreadOnly = false);

    /// <summary>
    /// Get recent notifications for dropdown (last 5)
    /// </summary>
    Task<List<NotificationDto>> GetRecentAsync(Guid userId, string userType, int count = 5);

    /// <summary>
    /// Get unread notification count
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, string userType);

    /// <summary>
    /// Mark a single notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    Task<int> MarkAllAsReadAsync(Guid userId, string userType);

    /// <summary>
    /// Delete a notification
    /// </summary>
    Task<bool> DeleteAsync(Guid notificationId);

    // ==================== Preferences ====================

    /// <summary>
    /// Get notification preferences for a user
    /// </summary>
    Task<NotificationPreferenceDto> GetPreferencesAsync(Guid userId, string userType);

    /// <summary>
    /// Update notification preferences
    /// </summary>
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid userId, 
        string userType, 
        NotificationPreferenceDto preferences);

    // ==================== Push Subscriptions ====================

    /// <summary>
    /// Register a push subscription
    /// </summary>
    Task<bool> RegisterPushSubscriptionAsync(
        Guid userId, 
        string userType, 
        RegisterPushSubscriptionDto subscription);

    /// <summary>
    /// Unregister a push subscription
    /// </summary>
    Task<bool> UnregisterPushSubscriptionAsync(string endpoint);

    // ==================== Convenience Methods ====================

    /// <summary>
    /// Send subscription expiry warning notification
    /// </summary>
    Task SendSubscriptionExpiryWarningAsync(
        Guid companyAdminId,
        Guid subscriptionId,
        string companyName,
        string planName,
        int daysUntilExpiry);

    /// <summary>
    /// Send security alert notification
    /// </summary>
    Task SendSecurityAlertAsync(
        Guid userId,
        string userType,
        string alertType,
        string message,
        string? ipAddress = null,
        string? deviceInfo = null);

    /// <summary>
    /// Send device limit reached notification
    /// </summary>
    Task SendDeviceLimitReachedAsync(
        Guid companyAdminId,
        Guid subscriptionId,
        string planName,
        int currentCount,
        int maxCount);

    /// <summary>
    /// Send promotional notification to all users of a type
    /// </summary>
    Task<int> SendPromotionalAsync(
        string userType,
        string title,
        string message,
        string? actionUrl = null);
}

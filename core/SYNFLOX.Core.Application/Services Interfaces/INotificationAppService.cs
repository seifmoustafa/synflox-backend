using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Notification;

namespace Application.Services;

/// <summary>
/// Service interface for notification operations.
/// Handles CRUD, bulk operations, and push notifications.
/// </summary>
public interface INotificationAppService
{
    #region Admin Operations

    /// <summary>
    /// Send notifications to selected companies (Admin only).
    /// Creates notifications for each company's admin user.
    /// </summary>
    Task<SendMessageResultDto> SendToCompaniesAsync(
        SendMessageRequestDto request,
        CancellationToken cancellationToken = default
    );

    #endregion

    #region Shared Operations (Admin & Client)

    /// <summary>
    /// Get notifications for the current user with pagination.
    /// </summary>
    Task<NotificationListDto> GetNotificationsAsync(
        Guid userId,
        string userType,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get recent notifications for header dropdown.
    /// </summary>
    Task<RecentNotificationsDto> GetRecentNotificationsAsync(
        Guid userId,
        string userType,
        int count = 5,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get unread notification count for badge.
    /// </summary>
    Task<int> GetUnreadCountAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Mark a single notification as read.
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for the user.
    /// </summary>
    Task<int> MarkAllAsReadAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Delete (soft delete) a notification.
    /// </summary>
    Task<bool> DeleteNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default
    );

    #endregion

    #region Preferences

    /// <summary>
    /// Get notification preferences for the user.
    /// </summary>
    Task<NotificationPreferenceDto> GetPreferencesAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Update notification preferences.
    /// </summary>
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid userId,
        string userType,
        NotificationPreferenceDto preferences,
        CancellationToken cancellationToken = default
    );

    #endregion

    #region Push Subscriptions (Mobile/Web)

    /// <summary>
    /// Register a push subscription for web push or FCM (Flutter/Mobile).
    /// The Endpoint field should contain the push endpoint (web) or device token (mobile).
    /// </summary>
    Task<bool> RegisterPushSubscriptionAsync(
        Guid userId,
        string userType,
        RegisterPushSubscriptionDto subscription,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Unregister a push subscription by endpoint/device token.
    /// </summary>
    Task<bool> UnregisterPushSubscriptionAsync(
        string endpoint,
        CancellationToken cancellationToken = default
    );

    #endregion

    #region Internal Operations

    /// <summary>
    /// Create a single notification (for internal use by other services).
    /// </summary>
    Task<NotificationDto> CreateNotificationAsync(
        CreateNotificationDto dto,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Clean up expired notifications.
    /// </summary>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);

    #endregion
}

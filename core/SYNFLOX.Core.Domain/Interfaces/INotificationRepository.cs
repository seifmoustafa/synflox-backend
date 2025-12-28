using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Notifications;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for Notification entity
/// </summary>
public interface INotificationRepository : IBaseRepository<Guid, Notification>
{
    /// <summary>
    /// Get notifications for a specific user with pagination
    /// </summary>
    Task<IEnumerable<Notification>> GetByUserIdAsync(
        Guid userId,
        string userType,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get total count of notifications for a user
    /// </summary>
    Task<int> GetTotalCountAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get unread notification count for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get recent notifications for dropdown
    /// </summary>
    Task<IEnumerable<Notification>> GetRecentAsync(
        Guid userId,
        string userType,
        int count = 5,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Mark a single notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    Task<int> MarkAllAsReadAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Soft delete expired notifications
    /// </summary>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for NotificationPreference entity
/// </summary>
public interface INotificationPreferenceRepository : IBaseRepository<Guid, NotificationPreference>
{
    /// <summary>
    /// Get preferences for a specific user
    /// </summary>
    Task<NotificationPreference?> GetByUserIdAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get or create preferences for a user (creates default if not exists)
    /// </summary>
    Task<NotificationPreference> GetOrCreateByUserIdAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Repository interface for PushSubscription entity.
/// For all platforms (web, android, ios, flutter), the Endpoint field stores the unique token.
/// </summary>
public interface IPushSubscriptionRepository : IBaseRepository<Guid, PushSubscription>
{
    /// <summary>
    /// Get active push subscriptions for a user
    /// </summary>
    Task<IEnumerable<PushSubscription>> GetActiveByUserIdAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get subscription by endpoint (FCM token, APNS token, or Web Push endpoint)
    /// </summary>
    Task<PushSubscription?> GetByEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Delete subscription by endpoint
    /// </summary>
    Task<bool> DeleteByEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get all subscriptions by platform
    /// </summary>
    Task<IEnumerable<PushSubscription>> GetByPlatformAsync(
        string platform,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Delete stale subscriptions (not used in 30 days)
    /// </summary>
    Task<int> DeleteStaleAsync(CancellationToken cancellationToken = default);
}

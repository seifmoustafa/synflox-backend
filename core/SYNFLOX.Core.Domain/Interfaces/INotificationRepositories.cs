using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Notifications;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for Notification entity
/// </summary>
public interface INotificationRepository : IBaseRepository<Guid, Notification>
{
    /// <summary>
    /// Get notifications for a user with pagination
    /// </summary>
    Task<IEnumerable<Notification>> GetByUserIdAsync(
        Guid userId, 
        string userType,
        int page = 1, 
        int pageSize = 20,
        bool unreadOnly = false);

    /// <summary>
    /// Get unread notification count for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, string userType);

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    Task<int> MarkAllAsReadAsync(Guid userId, string userType);

    /// <summary>
    /// Delete expired notifications (cleanup job)
    /// </summary>
    Task<int> DeleteExpiredAsync();

    /// <summary>
    /// Get recent notifications for a user (for dropdown)
    /// </summary>
    Task<IEnumerable<Notification>> GetRecentAsync(Guid userId, string userType, int count = 5);
}

/// <summary>
/// Repository interface for NotificationPreference entity
/// </summary>
public interface INotificationPreferenceRepository : IBaseRepository<Guid, NotificationPreference>
{
    /// <summary>
    /// Get preferences for a user, creating default if not exists
    /// </summary>
    Task<NotificationPreference> GetOrCreateByUserIdAsync(Guid userId, string userType);

    /// <summary>
    /// Get preferences by user ID
    /// </summary>
    Task<NotificationPreference?> GetByUserIdAsync(Guid userId, string userType);
}

/// <summary>
/// Repository interface for PushSubscription entity
/// </summary>
public interface IPushSubscriptionRepository : IBaseRepository<Guid, PushSubscription>
{
    /// <summary>
    /// Get all active push subscriptions for a user
    /// </summary>
    Task<IEnumerable<PushSubscription>> GetActiveByUserIdAsync(Guid userId, string userType);

    /// <summary>
    /// Get subscription by endpoint (for updates/deletes)
    /// </summary>
    Task<PushSubscription?> GetByEndpointAsync(string endpoint);

    /// <summary>
    /// Delete subscriptions by endpoint
    /// </summary>
    Task<bool> DeleteByEndpointAsync(string endpoint);

    /// <summary>
    /// Get all subscriptions for a platform (for platform-specific updates)
    /// </summary>
    Task<IEnumerable<PushSubscription>> GetByPlatformAsync(string platform);

    /// <summary>
    /// Clean up stale subscriptions (not used in 30 days)
    /// </summary>
    Task<int> DeleteStaleAsync();
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Notifications;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Notification entity
/// </summary>
public class NotificationRepository : BaseRepository<Guid, Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDBContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IEnumerable<Notification>> GetByUserIdAsync(
        Guid userId,
        string userType,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default
    )
    {
        var query = _dbSet.Where(n => !n.IsDeleted && n.UserId == userId && n.UserType == userType);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }
        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalCountAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Where(n => !n.IsDeleted && n.UserId == userId && n.UserType == userType)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Where(n => !n.IsDeleted && n.UserId == userId && n.UserType == userType && !n.IsRead)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Notification>> GetRecentAsync(
        Guid userId,
        string userType,
        int count = 5,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Where(n => !n.IsDeleted && n.UserId == userId && n.UserType == userType)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default
    )
    {
        var notification = await _dbSet
            .Where(n => !n.IsDeleted && n.Id == notificationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (notification != null && !notification.IsRead)
        {
            notification.MarkAsRead();
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<int> MarkAllAsReadAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        var notifications = await _dbSet
            .Where(n => !n.IsDeleted && n.UserId == userId && n.UserType == userType && !n.IsRead)
            .ToListAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }
        return notifications.Count;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _dbSet
            .Where(n => n.ExpiresAtUtc.HasValue && n.ExpiresAtUtc.Value < now)
            .ToListAsync(cancellationToken);
        foreach (var notification in expired)
        {
            notification.IsDeleted = true;
        }
        return expired.Count;
    }
}

/// <summary>
/// Repository implementation for NotificationPreference entity
/// </summary>
public class NotificationPreferenceRepository
    : BaseRepository<Guid, NotificationPreference>,
        INotificationPreferenceRepository
{
    public NotificationPreferenceRepository(ApplicationDBContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<NotificationPreference?> GetByUserIdAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Where(p => !p.IsDeleted && p.UserId == userId && p.UserType == userType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<NotificationPreference> GetOrCreateByUserIdAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        var preference = await GetByUserIdAsync(userId, userType, cancellationToken);
        if (preference == null)
        {
            preference = new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserType = userType,
            };
            await _dbSet.AddAsync(preference, cancellationToken);
        }
        return preference;
    }
}

/// <summary>
/// Repository implementation for PushSubscription entity.
/// Uses Endpoint field for all platform tokens (FCM, APNS, Web Push).
/// </summary>
public class PushSubscriptionRepository
    : BaseRepository<Guid, PushSubscription>,
        IPushSubscriptionRepository
{
    public PushSubscriptionRepository(ApplicationDBContext context)
        : base(context) { }

    /// <inheritdoc/>
    public async Task<IEnumerable<PushSubscription>> GetActiveByUserIdAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && s.IsActive && s.UserId == userId && s.UserType == userType)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PushSubscription?> GetByEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && s.Endpoint == endpoint)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteByEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetByEndpointAsync(endpoint, cancellationToken);
        if (subscription != null)
        {
            subscription.IsDeleted = true;
            subscription.IsActive = false;
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PushSubscription>> GetByPlatformAsync(
        string platform,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && s.IsActive && s.Platform == platform)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteStaleAsync(CancellationToken cancellationToken = default)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var stale = await _dbSet
            .Where(s =>
                !s.IsDeleted
                && s.IsActive
                && s.LastUsedAtUtc.HasValue
                && s.LastUsedAtUtc.Value < thirtyDaysAgo
            )
            .ToListAsync(cancellationToken);
        foreach (var subscription in stale)
        {
            subscription.IsActive = false;
        }
        return stale.Count;
    }
}

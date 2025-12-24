using System;
using System.Collections.Generic;
using System.Linq;
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
    public NotificationRepository(ApplicationDBContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Notification>> GetByUserIdAsync(
        Guid userId,
        string userType,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false)
    {
        var query = _dbSet
            .Where(n => !n.IsDeleted && 
                        n.UserId == userId && 
                        n.UserType == userType);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(Guid userId, string userType)
    {
        return await _dbSet
            .Where(n => !n.IsDeleted && 
                        n.UserId == userId && 
                        n.UserType == userType && 
                        !n.IsRead)
            .CountAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _dbSet
            .Where(n => !n.IsDeleted && n.Id == notificationId)
            .FirstOrDefaultAsync();

        if (notification != null && !notification.IsRead)
        {
            notification.MarkAsRead();
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<int> MarkAllAsReadAsync(Guid userId, string userType)
    {
        var notifications = await _dbSet
            .Where(n => !n.IsDeleted && 
                        n.UserId == userId && 
                        n.UserType == userType && 
                        !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        return notifications.Count;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _dbSet
            .Where(n => n.ExpiresAtUtc.HasValue && n.ExpiresAtUtc.Value < now)
            .ToListAsync();

        foreach (var notification in expired)
        {
            notification.IsDeleted = true;
        }

        return expired.Count;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Notification>> GetRecentAsync(Guid userId, string userType, int count = 5)
    {
        return await _dbSet
            .Where(n => !n.IsDeleted && 
                        n.UserId == userId && 
                        n.UserType == userType)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }
}

/// <summary>
/// Repository implementation for NotificationPreference entity
/// </summary>
public class NotificationPreferenceRepository : BaseRepository<Guid, NotificationPreference>, INotificationPreferenceRepository
{
    public NotificationPreferenceRepository(ApplicationDBContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<NotificationPreference?> GetByUserIdAsync(Guid userId, string userType)
    {
        return await _dbSet
            .Where(p => !p.IsDeleted && 
                        p.UserId == userId && 
                        p.UserType == userType)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<NotificationPreference> GetOrCreateByUserIdAsync(Guid userId, string userType)
    {
        var preference = await GetByUserIdAsync(userId, userType);
        
        if (preference == null)
        {
            preference = new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserType = userType
            };
            await _dbSet.AddAsync(preference);
        }

        return preference;
    }
}

/// <summary>
/// Repository implementation for PushSubscription entity
/// </summary>
public class PushSubscriptionRepository : BaseRepository<Guid, PushSubscription>, IPushSubscriptionRepository
{
    public PushSubscriptionRepository(ApplicationDBContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PushSubscription>> GetActiveByUserIdAsync(Guid userId, string userType)
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && 
                        s.IsActive &&
                        s.UserId == userId && 
                        s.UserType == userType)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<PushSubscription?> GetByEndpointAsync(string endpoint)
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && s.Endpoint == endpoint)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteByEndpointAsync(string endpoint)
    {
        var subscription = await GetByEndpointAsync(endpoint);
        if (subscription != null)
        {
            subscription.IsDeleted = true;
            subscription.IsActive = false;
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PushSubscription>> GetByPlatformAsync(string platform)
    {
        return await _dbSet
            .Where(s => !s.IsDeleted && 
                        s.IsActive && 
                        s.Platform == platform)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> DeleteStaleAsync()
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var stale = await _dbSet
            .Where(s => !s.IsDeleted && 
                        s.IsActive &&
                        s.LastUsedAtUtc.HasValue && 
                        s.LastUsedAtUtc.Value < thirtyDaysAgo)
            .ToListAsync();

        foreach (var subscription in stale)
        {
            subscription.IsActive = false;
        }

        return stale.Count;
    }
}

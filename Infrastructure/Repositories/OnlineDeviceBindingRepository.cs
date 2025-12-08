using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.OnlineAccess;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OnlineDeviceBindingRepository : BaseRepository<Guid, OnlineDeviceBinding>, IOnlineDeviceBindingRepository
{
    public OnlineDeviceBindingRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<OnlineDeviceBinding?> GetByFingerprintAsync(Guid subscriptionId, string fingerprint, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.Token)
            .FirstOrDefaultAsync(d => d.SubscriptionId == subscriptionId && 
                                     d.DeviceFingerprint == fingerprint, 
                                 cancellationToken);
    }

    public async Task<IEnumerable<OnlineDeviceBinding>> GetBySubscriptionIdAsync(Guid subscriptionId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(d => d.Token)
            .Where(d => d.SubscriptionId == subscriptionId);

        if (!includeInactive)
        {
            query = query.Where(d => d.Status == OnlineDeviceStatus.Active);
        }

        return await query
            .OrderByDescending(d => d.LastSeenAtUtc ?? d.FirstSeenAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OnlineDeviceBinding>> GetByTokenIdAsync(Guid tokenId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => d.TokenId == tokenId);

        if (!includeInactive)
        {
            query = query.Where(d => d.Status == OnlineDeviceStatus.Active);
        }

        return await query
            .OrderByDescending(d => d.LastSeenAtUtc ?? d.FirstSeenAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveDeviceCountAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(d => d.SubscriptionId == subscriptionId && 
                           d.Status == OnlineDeviceStatus.Active, 
                       cancellationToken);
    }

    public async Task<bool> IsDeviceRegisteredAsync(Guid subscriptionId, string fingerprint, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(d => d.SubscriptionId == subscriptionId && 
                          d.DeviceFingerprint == fingerprint &&
                          d.Status == OnlineDeviceStatus.Active, 
                     cancellationToken);
    }

    public async Task UpdateLastSeenAsync(Guid deviceId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(d => d.Id == deviceId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.LastSeenAtUtc, DateTime.UtcNow)
                .SetProperty(d => d.LastIpAddress, ipAddress)
                .SetProperty(d => d.LastUserAgent, userAgent),
                cancellationToken);
    }

    public async Task IncrementApiCallCountAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(d => d.Id == deviceId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.ApiCallCount, d => d.ApiCallCount + 1)
                .SetProperty(d => d.LastSeenAtUtc, DateTime.UtcNow),
                cancellationToken);
    }
}

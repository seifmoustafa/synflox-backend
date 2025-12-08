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

public class OnlineClientTokenRepository : BaseRepository<Guid, OnlineClientToken>, IOnlineClientTokenRepository
{
    public OnlineClientTokenRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<OnlineClientToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Company)
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IEnumerable<OnlineClientToken>> GetByCompanyIdAsync(Guid companyId, bool includeRevoked = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(t => t.CompanyId == companyId);

        if (!includeRevoked)
        {
            query = query.Where(t => t.Status != ClientTokenStatus.Revoked);
        }

        return await query
            .OrderByDescending(t => t.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OnlineClientToken>> GetBySubscriptionIdAsync(Guid subscriptionId, bool includeRevoked = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(t => t.Company)
            .Where(t => t.SubscriptionId == subscriptionId);

        if (!includeRevoked)
        {
            query = query.Where(t => t.Status != ClientTokenStatus.Revoked);
        }

        return await query
            .OrderByDescending(t => t.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<OnlineClientToken?> GetWithDevicesAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Company)
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .Include(t => t.BoundDevices.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);
    }

    public async Task<OnlineClientToken?> GetActiveTokenForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Company)
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(t => t.SubscriptionId == subscriptionId && 
                       t.Status == ClientTokenStatus.Active &&
                       t.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedTimestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateLastUsedAsync(Guid tokenId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(t => t.Id == tokenId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.LastUsedAtUtc, DateTime.UtcNow)
                .SetProperty(t => t.UsageCount, t => t.UsageCount + 1)
                .SetProperty(t => t.LastUsedFromIp, ipAddress)
                .SetProperty(t => t.LastUsedUserAgent, userAgent),
                cancellationToken);
    }

    public async Task<IEnumerable<OnlineClientToken>> GetExpiringTokensAsync(int daysFromNow, CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.AddDays(daysFromNow);
        
        return await _dbSet
            .Include(t => t.Company)
            .Include(t => t.Subscription)
            .Where(t => t.Status == ClientTokenStatus.Active &&
                       t.ExpiresAtUtc <= targetDate &&
                       t.ExpiresAtUtc > DateTime.UtcNow)
            .OrderBy(t => t.ExpiresAtUtc)
            .ToListAsync(cancellationToken);
    }
}

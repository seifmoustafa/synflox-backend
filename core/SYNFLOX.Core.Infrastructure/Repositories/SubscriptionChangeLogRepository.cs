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

public class SubscriptionChangeLogRepository : BaseRepository<Guid, SubscriptionChangeLog>, ISubscriptionChangeLogRepository
{
    public SubscriptionChangeLogRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SubscriptionChangeLog>> GetPendingChangesAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Plan)
            .Where(c => c.SubscriptionId == subscriptionId && 
                       !c.IsApplied && 
                       !c.IsCancelled)
            .OrderBy(c => c.EffectiveDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SubscriptionChangeLog>> GetPendingPlanChangesAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Subscription)
            .Where(c => c.PlanId == planId && 
                       !c.IsApplied && 
                       !c.IsCancelled)
            .OrderBy(c => c.EffectiveDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SubscriptionChangeLog>> GetChangesReadyToApplyAsync(CancellationToken cancellationToken = default)
    {
        return await GetChangesReadyToApplyAsync(DateTime.UtcNow, cancellationToken);
    }

    public async Task<IEnumerable<SubscriptionChangeLog>> GetChangesReadyToApplyAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Subscription)
                .ThenInclude(s => s.Company)
            .Include(c => c.Subscription)
                .ThenInclude(s => s.Plan)
            .Include(c => c.Plan)
            .Where(c => !c.IsApplied && 
                       !c.IsCancelled && 
                       c.EffectiveDateUtc <= asOfUtc)
            .OrderBy(c => c.EffectiveDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SubscriptionChangeLog>> GetByChangeTypeAsync(Guid subscriptionId, string changeType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.SubscriptionId == subscriptionId && 
                       c.ChangeType == changeType)
            .OrderByDescending(c => c.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SubscriptionChangeLog>> GetAppliedChangesAsync(Guid subscriptionId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.SubscriptionId == subscriptionId && 
                       c.IsApplied && 
                       c.AppliedAtUtc >= fromUtc && 
                       c.AppliedAtUtc <= toUtc)
            .OrderByDescending(c => c.AppliedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsAppliedAsync(Guid changeId, string appliedBy, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(c => c.Id == changeId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsApplied, true)
                .SetProperty(c => c.AppliedAtUtc, DateTime.UtcNow)
                .SetProperty(c => c.AppliedBy, appliedBy),
                cancellationToken);
    }

    public async Task CancelChangeAsync(Guid changeId, string reason, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(c => c.Id == changeId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsCancelled, true)
                .SetProperty(c => c.CancellationReason, reason),
                cancellationToken);
    }

    public async Task<IEnumerable<SubscriptionChangeLog>> GetUnnotifiedChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Subscription)
                .ThenInclude(s => s.Company)
            .Where(c => !c.CustomerNotified && 
                       !c.IsApplied && 
                       !c.IsCancelled)
            .OrderBy(c => c.EffectiveDateUtc)
            .ToListAsync(cancellationToken);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Interfaces.Repositories;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AccessTimeWindow entity.
/// </summary>
public class AccessTimeWindowRepository : BaseRepository<Guid, AccessTimeWindow>, IAccessTimeWindowRepository
{
    public AccessTimeWindowRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<List<AccessTimeWindow>> GetByPlanIdAsync(Guid planId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(w => w.PlanId == planId && !w.IsDeleted);
        
        if (!includeInactive)
            query = query.Where(w => w.IsActive);
        
        return await query
            .OrderBy(w => w.DisplayOrder)
            .ThenBy(w => w.DayOfWeek)
            .ThenBy(w => w.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AccessTimeWindow>> GetActiveByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await GetByPlanIdAsync(planId, false, cancellationToken);
    }

    public async Task<bool> IsWithinActiveWindowAsync(Guid planId, string? companyTimezoneId = null, CancellationToken cancellationToken = default)
    {
        var windows = await GetActiveByPlanIdAsync(planId, cancellationToken);
        
        if (!windows.Any())
        {
            // No time windows defined = always allowed
            return true;
        }

        var utcNow = DateTime.UtcNow;
        return windows.Any(w => w.IsWithinWindow(utcNow, companyTimezoneId));
    }

    public async Task DeleteByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var windows = await _dbSet
            .Where(w => w.PlanId == planId)
            .ToListAsync(cancellationToken);

        foreach (var window in windows)
        {
            window.IsDeleted = true;
            window.DeletedTimestamp = DateTime.UtcNow;
        }
    }

    public async Task BulkCreateAsync(Guid planId, IEnumerable<AccessTimeWindow> windows, CancellationToken cancellationToken = default)
    {
        foreach (var window in windows)
        {
            window.PlanId = planId;
            window.Id = Guid.NewGuid();
            window.CreatedTimestamp = DateTime.UtcNow;
            await _dbSet.AddAsync(window, cancellationToken);
        }
    }
}

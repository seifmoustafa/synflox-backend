using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SubscriptionRepository : BaseRepository<Guid, Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Plan)
            .Include(s => s.NextPlan)
            .Where(s => !s.IsDeleted && s.CompanyId == companyId && s.IsActive && !s.IsExpired)
            .OrderByDescending(s => s.StartDateUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetActiveByCompanyAndPlanAsync(Guid companyId, Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Where(s => !s.IsDeleted && s.CompanyId == companyId && s.PlanId == planId && s.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Plan)
            .Include(s => s.NextPlan)
            .Where(s => !s.IsDeleted && s.CompanyId == companyId)
            .OrderByDescending(s => s.StartDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetExpiredSubscriptionsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Plan)
            .Include(s => s.Company)
            .Where(s => !s.IsDeleted 
                && s.IsActive 
                && !s.IsExpired 
                && s.ExpiryDateUtc.AddDays(s.Plan.GracePeriodDays) < utcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsDueForActivationAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Plan)
            .Include(s => s.NextPlan)
            .Include(s => s.Company)
            .Where(s => !s.IsDeleted 
                && !s.IsActive 
                && s.NextPlanId != null 
                && s.NextPlanStartDateUtc != null 
                && s.NextPlanStartDateUtc <= utcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsForAutoRenewalAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Plan)
            .Include(s => s.Company)
            .Where(s => !s.IsDeleted 
                && s.IsActive 
                && !s.IsExpired 
                && s.AutoRenew 
                && s.NextPlanId == null 
                && s.ExpiryDateUtc <= utcNow.AddDays(1)) // 1 day before expiry
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingActiveSubscriptionAsync(
        Guid companyId, 
        Guid planId, 
        DateTime startDate, 
        DateTime expiryDate, 
        Guid? excludeSubscriptionId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Subscription>()
            .Where(s => !s.IsDeleted 
                && s.CompanyId == companyId 
                && s.PlanId == planId 
                && s.IsActive 
                && !s.IsExpired
                && ((s.StartDateUtc <= expiryDate && s.ExpiryDateUtc >= startDate))); // Overlap check

        if (excludeSubscriptionId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSubscriptionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Subscription?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Company)
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanPrices)
            // Include Plan Projects with their Modules
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanProjects)
                    .ThenInclude(pp => pp.Project)
                        .ThenInclude(p => p.ProjectModules)
                            .ThenInclude(pm => pm.Module)
            // Include Plan Standalone Modules
            .Include(s => s.Plan)
                .ThenInclude(p => p.PlanModules)
                    .ThenInclude(pm => pm.Module)
            .Include(s => s.NextPlan)
            .Include(s => s.ParentSubscription)
            .Where(s => !s.IsDeleted && s.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasActiveSubscriptionsForPlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Subscription>()
            .Where(s => !s.IsDeleted 
                && s.PlanId == planId 
                && s.IsActive
                && !s.IsExpired
                && s.ExpiryDateUtc > now)
            .AnyAsync(cancellationToken);
    }
}

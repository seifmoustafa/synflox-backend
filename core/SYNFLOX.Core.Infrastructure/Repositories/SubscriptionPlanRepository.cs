using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SubscriptionPlanRepository : BaseRepository<Guid, SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<SubscriptionPlan?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionPlan>()
            .Where(p => !p.IsDeleted && p.Name.ToLower() == name.ToLower())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionPlan>()
            .Include(p => p.PlanPrices)
            .Include(p => p.PlanProjects)
                .ThenInclude(pp => pp.Project)
                    .ThenInclude(proj => proj.ProjectModules)
                        .ThenInclude(pm => pm.Module)
            .Include(p => p.PlanModules)
                .ThenInclude(pm => pm.Module)
            .Where(p => !p.IsDeleted && p.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<SubscriptionPlan>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionPlan>()
            .Include(p => p.PlanPrices)
            .Include(p => p.PlanProjects)
                .ThenInclude(pp => pp.Project)
            .Include(p => p.PlanModules)
                .ThenInclude(pm => pm.Module)
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal?> GetPriceAsync(Guid planId, Currency currency, CancellationToken cancellationToken = default)
    {
        var planPrice = await _context.Set<PlanPrice>()
            .Where(pp => pp.PlanId == planId && pp.Currency == currency)
            .FirstOrDefaultAsync(cancellationToken);

        return planPrice?.Amount;
    }

    public async Task<SubscriptionPlan?> GetWithProjectsAndModulesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionPlan>()
            .Include(p => p.PlanProjects)
                .ThenInclude(pp => pp.Project)
                    .ThenInclude(proj => proj.ProjectModules)
                        .ThenInclude(pm => pm.Module)
            .Include(p => p.PlanModules)
                .ThenInclude(pm => pm.Module)
                    .ThenInclude(m => m.ProjectModules)
            .Include(p => p.DefaultFallbackPlan)
            .Where(p => !p.IsDeleted && p.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<SubscriptionPlan>> GetFreeTierPlansAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionPlan>()
            .Include(p => p.PlanPrices)
            .Where(p => !p.IsDeleted && p.IsFreeTier)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    #region Delete Cascade Support

    public async Task<int> GetChildPlansCountAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionPlan>()
            .CountAsync(p => p.ParentPlanId == planId && !p.IsDeleted, cancellationToken);
    }

    public async Task<int> GetFallbackReferencesCountAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionPlan>()
            .CountAsync(p => p.DefaultFallbackPlanId == planId && !p.IsDeleted, cancellationToken);
    }

    public async Task NullifyChildPlanReferencesAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var childPlans = await _context.Set<SubscriptionPlan>()
            .Where(p => p.ParentPlanId == planId && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var child in childPlans)
        {
            child.ParentPlanId = null;
            child.UpdatedTimestamp = DateTime.UtcNow;
        }
    }

    public async Task NullifyFallbackReferencesAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var referencingPlans = await _context.Set<SubscriptionPlan>()
            .Where(p => p.DefaultFallbackPlanId == planId && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var plan in referencingPlans)
        {
            plan.DefaultFallbackPlanId = null;
            plan.UpdatedTimestamp = DateTime.UtcNow;
        }
    }

    #endregion
}

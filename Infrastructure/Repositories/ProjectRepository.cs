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

public class ProjectRepository : BaseRepository<Guid, Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<Project?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Project>()
            .Where(p => !p.IsDeleted && p.Name.ToLower() == name.ToLower())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetWithModulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Project>()
            .Include(p => p.ProjectModules)
                .ThenInclude(pm => pm.Module)
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdWithModulesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Project>()
            .Include(p => p.ProjectModules)
                .ThenInclude(pm => pm.Module)
            .Where(p => !p.IsDeleted && p.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsUsedInActiveSubscriptionsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Subscription>()
            .Where(s => !s.IsDeleted 
                && s.Plan.PlanProjects.Any(pp => pp.ProjectId == projectId)
                && s.IsActive
                && !s.IsExpired
                && s.ExpiryDateUtc > now)
            .AnyAsync(cancellationToken);
    }

    #region Delete Cascade Support

    public async Task<int> GetAffectedPlansCountAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanProjects
            .CountAsync(pp => pp.ProjectId == projectId && !pp.Plan.IsDeleted, cancellationToken);
    }

    public async Task<List<string>> GetAffectedPlanNamesAsync(Guid projectId, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _context.PlanProjects
            .Where(pp => pp.ProjectId == projectId && !pp.Plan.IsDeleted)
            .Select(pp => pp.Plan.Name)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasRelatedRecordsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        // Check PlanProjects
        var hasPlans = await _context.PlanProjects
            .AnyAsync(pp => pp.ProjectId == projectId && !pp.Plan.IsDeleted, cancellationToken);
        if (hasPlans) return true;

        // Check PlanEntitlements (direct or as parent)
        var hasEntitlements = await _context.PlanEntitlements
            .AnyAsync(e => (e.ProjectId == projectId || e.ParentProjectId == projectId) && !e.IsDeleted, cancellationToken);
        
        return hasEntitlements;
    }

    public async Task RemoveFromAllPlansAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        // Hard delete from join table (not an entity with soft delete)
        var planProjects = await _context.PlanProjects
            .Where(pp => pp.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        
        _context.PlanProjects.RemoveRange(planProjects);
    }

    #endregion
}

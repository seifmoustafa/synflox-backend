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

public class ModuleRepository : BaseRepository<Guid, Module>, IModuleRepository
{
    public ModuleRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<Module?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Module>()
            .Where(m => !m.IsDeleted && m.Name.ToLower() == name.ToLower())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Module>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Module>()
            .Where(m => !m.IsDeleted && m.ProjectModules.Any(pm => pm.ProjectId == projectId))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUsedInProjectsOrPlansAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        // Check if module is used in any projects
        var usedInProjects = await _context.Set<ProjectModule>()
            .AnyAsync(pm => pm.ModuleId == moduleId, cancellationToken);

        if (usedInProjects)
            return true;

        // Check if module is used in any subscription plans
        var usedInPlans = await _context.Set<PlanModule>()
            .AnyAsync(pm => pm.ModuleId == moduleId, cancellationToken);

        return usedInPlans;
    }

    #region Delete Cascade Support

    public async Task<int> GetAffectedProjectsCountAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectModules
            .CountAsync(pm => pm.ModuleId == moduleId && !pm.Project.IsDeleted, cancellationToken);
    }

    public async Task<List<string>> GetAffectedProjectNamesAsync(Guid moduleId, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectModules
            .Where(pm => pm.ModuleId == moduleId && !pm.Project.IsDeleted)
            .Select(pm => pm.Project.Name)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAffectedPlansCountAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanModules
            .CountAsync(pm => pm.ModuleId == moduleId && !pm.Plan.IsDeleted, cancellationToken);
    }

    public async Task<List<string>> GetAffectedPlanNamesAsync(Guid moduleId, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _context.PlanModules
            .Where(pm => pm.ModuleId == moduleId && !pm.Plan.IsDeleted)
            .Select(pm => pm.Plan.Name)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasRelatedRecordsAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        // Check ProjectModules
        var hasProjects = await _context.ProjectModules
            .AnyAsync(pm => pm.ModuleId == moduleId && !pm.Project.IsDeleted, cancellationToken);
        if (hasProjects) return true;

        // Check PlanModules
        var hasPlans = await _context.PlanModules
            .AnyAsync(pm => pm.ModuleId == moduleId && !pm.Plan.IsDeleted, cancellationToken);
        if (hasPlans) return true;

        // Check PlanEntitlements
        var hasEntitlements = await _context.PlanEntitlements
            .AnyAsync(e => e.ModuleId == moduleId && !e.IsDeleted, cancellationToken);
        
        return hasEntitlements;
    }

    public async Task RemoveFromAllProjectsAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        // Hard delete from join table (not an entity with soft delete)
        var projectModules = await _context.ProjectModules
            .Where(pm => pm.ModuleId == moduleId)
            .ToListAsync(cancellationToken);
        
        _context.ProjectModules.RemoveRange(projectModules);
    }

    public async Task RemoveFromAllPlansAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        // Hard delete from join table (not an entity with soft delete)
        var planModules = await _context.PlanModules
            .Where(pm => pm.ModuleId == moduleId)
            .ToListAsync(cancellationToken);
        
        _context.PlanModules.RemoveRange(planModules);
    }

    #endregion
}

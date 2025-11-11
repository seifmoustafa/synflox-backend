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
}

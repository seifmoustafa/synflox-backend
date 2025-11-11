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
}

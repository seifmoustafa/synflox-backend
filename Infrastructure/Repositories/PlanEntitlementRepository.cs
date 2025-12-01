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

/// <summary>
/// Repository implementation for PlanEntitlement operations
/// </summary>
public class PlanEntitlementRepository : BaseRepository<Guid, PlanEntitlement>, IPlanEntitlementRepository
{
    public PlanEntitlementRepository(ApplicationDBContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PlanEntitlement>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanEntitlements
            .Where(e => e.PlanId == planId && !e.IsDeleted)
            .OrderBy(e => e.TargetType)
            .ThenBy(e => e.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PlanEntitlement>> GetByPlanIdWithDetailsAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanEntitlements
            .Include(e => e.Plan)
            .Include(e => e.Project)
            .Include(e => e.Module)
            .Where(e => e.PlanId == planId && !e.IsDeleted)
            .OrderBy(e => e.ProjectId.HasValue ? 0 : 1) // Projects first
            .ThenBy(e => e.Project != null ? e.Project.Name : e.Module != null ? e.Module.Name : "")
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlanEntitlement?> GetByPlanAndTargetAsync(Guid planId, Guid? projectId, Guid? moduleId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanEntitlements
            .Include(e => e.Plan)
            .Include(e => e.Project)
            .Include(e => e.Module)
            .FirstOrDefaultAsync(e => 
                e.PlanId == planId && 
                e.ProjectId == projectId && 
                e.ModuleId == moduleId && 
                !e.IsDeleted, 
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid planId, Guid? projectId, Guid? moduleId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanEntitlements
            .AnyAsync(e => 
                e.PlanId == planId && 
                e.ProjectId == projectId && 
                e.ModuleId == moduleId && 
                !e.IsDeleted, 
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanEntitlements
            .CountAsync(e => e.PlanId == planId && !e.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAllByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var entitlements = await _context.PlanEntitlements
            .Where(e => e.PlanId == planId && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var entitlement in entitlements)
        {
            entitlement.IsDeleted = true;
            entitlement.IsActive = false;
            entitlement.DeletedTimestamp = now;
            entitlement.UpdatedTimestamp = now;
        }
    }

    /// <inheritdoc />
    public async Task CopyEntitlementsAsync(Guid sourcePlanId, Guid targetPlanId, CancellationToken cancellationToken = default)
    {
        var sourceEntitlements = await GetByPlanIdAsync(sourcePlanId, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var source in sourceEntitlements)
        {
            var copy = new PlanEntitlement
            {
                Id = Guid.NewGuid(),
                PlanId = targetPlanId,
                ProjectId = source.ProjectId,
                ModuleId = source.ModuleId,
                AccessLevel = source.AccessLevel,
                CanCreate = source.CanCreate,
                CanRead = source.CanRead,
                CanUpdate = source.CanUpdate,
                CanDelete = source.CanDelete,
                CanExport = source.CanExport,
                DisplayInMenu = source.DisplayInMenu,
                Features = source.Features,
                IsActive = true,
                IsDeleted = false,
                CreatedTimestamp = now
            };

            await _context.PlanEntitlements.AddAsync(copy, cancellationToken);
        }
    }
}

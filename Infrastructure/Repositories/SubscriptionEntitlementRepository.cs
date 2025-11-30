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

/// <summary>
/// Repository implementation for SubscriptionEntitlement entity
/// </summary>
public class SubscriptionEntitlementRepository : BaseRepository<Guid, SubscriptionEntitlement>, ISubscriptionEntitlementRepository
{
    public SubscriptionEntitlementRepository(ApplicationDBContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionEntitlement>> GetBySubscriptionIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .Where(e => !e.IsDeleted && e.IsActive && e.SubscriptionId == subscriptionId)
            .OrderBy(e => e.ProjectId)
            .ThenBy(e => e.ModuleId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionEntitlement>> GetBySubscriptionWithDetailsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .Include(e => e.Project)
            .Include(e => e.Module)
            .Include(e => e.GrantedByAdmin)
            .Where(e => !e.IsDeleted && e.IsActive && e.SubscriptionId == subscriptionId)
            .OrderBy(e => e.Project != null ? e.Project.Name : "")
            .ThenBy(e => e.Module != null ? e.Module.Name : "")
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionEntitlement?> GetBySubscriptionAndProjectAsync(
        Guid subscriptionId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .Include(e => e.Project)
            .Where(e => !e.IsDeleted 
                && e.IsActive 
                && e.SubscriptionId == subscriptionId 
                && e.ProjectId == projectId
                && e.ModuleId == null) // Full project grant
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionEntitlement?> GetBySubscriptionProjectAndModuleAsync(
        Guid subscriptionId,
        Guid projectId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .Include(e => e.Project)
            .Include(e => e.Module)
            .Where(e => !e.IsDeleted 
                && e.IsActive 
                && e.SubscriptionId == subscriptionId 
                && e.ProjectId == projectId
                && e.ModuleId == moduleId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionEntitlement?> GetBySubscriptionAndModuleAsync(
        Guid subscriptionId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .Include(e => e.Module)
            .Where(e => !e.IsDeleted 
                && e.IsActive 
                && e.SubscriptionId == subscriptionId 
                && e.ProjectId == null  // Standalone module
                && e.ModuleId == moduleId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasProjectAccessAsync(
        Guid subscriptionId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .AnyAsync(e => !e.IsDeleted 
                && e.IsActive 
                && e.SubscriptionId == subscriptionId 
                && e.ProjectId == projectId
                && (!e.ExpiresAt.HasValue || e.ExpiresAt > DateTime.UtcNow), 
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasModuleAccessAsync(
        Guid subscriptionId,
        Guid projectId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        // Check for direct module grant OR full project grant
        return await _context.Set<SubscriptionEntitlement>()
            .AnyAsync(e => !e.IsDeleted 
                && e.IsActive 
                && e.SubscriptionId == subscriptionId 
                && e.ProjectId == projectId
                && (e.ModuleId == moduleId || (e.ModuleId == null && e.GrantType == EntitlementGrantType.FullProject))
                && (!e.ExpiresAt.HasValue || e.ExpiresAt > DateTime.UtcNow), 
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasStandaloneModuleAccessAsync(
        Guid subscriptionId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .AnyAsync(e => !e.IsDeleted 
                && e.IsActive 
                && e.SubscriptionId == subscriptionId 
                && e.ProjectId == null
                && e.ModuleId == moduleId
                && (!e.ExpiresAt.HasValue || e.ExpiresAt > DateTime.UtcNow), 
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionEntitlement>> GetBySourceAsync(
        Guid subscriptionId,
        EntitlementSource source,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .Include(e => e.Project)
            .Include(e => e.Module)
            .Where(e => !e.IsDeleted && e.SubscriptionId == subscriptionId && e.Source == source)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionEntitlement>> GetCustomEntitlementsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .Include(e => e.Project)
            .Include(e => e.Module)
            .Include(e => e.GrantedByAdmin)
            .Where(e => !e.IsDeleted && e.IsActive && e.SubscriptionId == subscriptionId && e.IsCustom)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionEntitlement>> GetExpiringEntitlementsAsync(
        int withinDays,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.AddDays(withinDays);
        return await _context.Set<SubscriptionEntitlement>()
            .Include(e => e.Subscription)
                .ThenInclude(s => s.Company)
            .Include(e => e.Project)
            .Include(e => e.Module)
            .Where(e => !e.IsDeleted 
                && e.IsActive 
                && e.ExpiresAt.HasValue 
                && e.ExpiresAt <= deadline
                && e.ExpiresAt > DateTime.UtcNow)
            .OrderBy(e => e.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeactivateAllForSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var entitlements = await _context.Set<SubscriptionEntitlement>()
            .Where(e => !e.IsDeleted && e.SubscriptionId == subscriptionId)
            .ToListAsync(cancellationToken);

        foreach (var entitlement in entitlements)
        {
            entitlement.IsActive = false;
            entitlement.UpdatedTimestamp = DateTime.UtcNow;
        }

        return entitlements.Count;
    }

    /// <inheritdoc />
    public async Task BulkInsertAsync(
        IEnumerable<SubscriptionEntitlement> entitlements,
        CancellationToken cancellationToken = default)
    {
        await _context.Set<SubscriptionEntitlement>().AddRangeAsync(entitlements, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountBySubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionEntitlement>()
            .CountAsync(e => !e.IsDeleted && e.IsActive && e.SubscriptionId == subscriptionId, cancellationToken);
    }
}

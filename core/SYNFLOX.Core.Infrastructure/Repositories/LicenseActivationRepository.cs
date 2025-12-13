using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces.Repositories;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for license activation management.
/// </summary>
public class LicenseActivationRepository : BaseRepository<Guid, LicenseActivation>, ILicenseActivationRepository
{
    public LicenseActivationRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<List<LicenseActivation>> GetBySubscriptionAsync(
        Guid subscriptionId,
        bool includeDeactivated = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(a => a.SubscriptionId == subscriptionId && !a.IsDeleted);
        
        if (!includeDeactivated)
        {
            query = query.Where(a => a.IsActive);
        }

        return await query
            .OrderByDescending(a => a.LastSeenAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LicenseActivation>> GetByCompanyAsync(
        Guid companyId,
        bool includeDeactivated = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(a => a.CompanyId == companyId && !a.IsDeleted);

        if (!includeDeactivated)
        {
            query = query.Where(a => a.IsActive);
        }

        return await query
            .OrderByDescending(a => a.LastSeenAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<LicenseActivation?> GetByMachineHashAsync(
        Guid subscriptionId,
        string machineHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.SubscriptionId == subscriptionId 
                     && a.MachineHash == machineHash 
                     && !a.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsMachineActivatedAsync(
        Guid subscriptionId,
        string machineHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(a => a.SubscriptionId == subscriptionId 
                        && a.MachineHash == machineHash 
                        && a.IsActive 
                        && !a.IsDeleted, 
                cancellationToken);
    }

    public async Task<int> GetActiveActivationCountAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(a => a.SubscriptionId == subscriptionId 
                          && a.IsActive 
                          && !a.IsDeleted, 
                cancellationToken);
    }

    public async Task<List<LicenseActivation>> GetRecentlyActiveDevicesAsync(
        Guid subscriptionId,
        int withinMinutes,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-withinMinutes);
        
        return await _dbSet
            .Where(a => a.SubscriptionId == subscriptionId 
                     && a.IsActive 
                     && !a.IsDeleted
                     && a.LastSeenAtUtc >= cutoff)
            .OrderByDescending(a => a.LastSeenAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateLastSeenAsync(
        Guid activationId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var activation = await _dbSet.FindAsync(new object[] { activationId }, cancellationToken);
        if (activation != null)
        {
            activation.LastSeenAtUtc = DateTime.UtcNow;
            activation.ValidationCount++;
            
            if (!string.IsNullOrEmpty(ipAddress))
                activation.LastIpAddress = ipAddress;
            
            if (!string.IsNullOrEmpty(userAgent))
                activation.LastUserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent;
        }
    }

    public async Task DeactivateAllForSubscriptionAsync(
        Guid subscriptionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var activations = await _dbSet
            .Where(a => a.SubscriptionId == subscriptionId && a.IsActive && !a.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var activation in activations)
        {
            activation.IsActive = false;
            activation.DeactivatedAtUtc = DateTime.UtcNow;
            activation.DeactivationReason = reason;
        }
    }

    public async Task DeactivateDeviceAsync(
        Guid activationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var activation = await _dbSet.FindAsync(new object[] { activationId }, cancellationToken);
        if (activation != null)
        {
            activation.IsActive = false;
            activation.DeactivatedAtUtc = DateTime.UtcNow;
            activation.DeactivationReason = reason;
        }
    }

    public async Task IncrementHardwareChangeAsync(
        Guid activationId,
        CancellationToken cancellationToken = default)
    {
        var activation = await _dbSet.FindAsync(new object[] { activationId }, cancellationToken);
        if (activation != null)
        {
            activation.HardwareChangeCount++;
            activation.LastHardwareChangeAtUtc = DateTime.UtcNow;
        }
    }
}

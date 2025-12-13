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
/// Repository for device replacement request management.
/// </summary>
public class DeviceReplacementRequestRepository : BaseRepository<Guid, DeviceReplacementRequest>, IDeviceReplacementRequestRepository
{
    public DeviceReplacementRequestRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<List<DeviceReplacementRequest>> GetPendingByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Subscription)
                .ThenInclude(s => s!.Plan)
            .Include(r => r.OldActivation)
            .Where(r => r.CompanyId == companyId && 
                       r.Status == ReplacementRequestStatus.Pending &&
                       r.ExpiresAtUtc > DateTime.UtcNow &&
                       !r.IsDeleted)
            .OrderByDescending(r => r.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeviceReplacementRequest>> GetPendingBySubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.OldActivation)
            .Where(r => r.SubscriptionId == subscriptionId && 
                       r.Status == ReplacementRequestStatus.Pending &&
                       r.ExpiresAtUtc > DateTime.UtcNow &&
                       !r.IsDeleted)
            .OrderByDescending(r => r.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceReplacementRequest?> GetPendingByMachineHashAsync(
        Guid subscriptionId,
        string machineHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.SubscriptionId == subscriptionId && 
                                     r.NewMachineHash == machineHash &&
                                     r.Status == ReplacementRequestStatus.Pending &&
                                     r.ExpiresAtUtc > DateTime.UtcNow &&
                                     !r.IsDeleted, 
                                cancellationToken);
    }

    public async Task<List<DeviceReplacementRequest>> GetBySubscriptionAsync(
        Guid subscriptionId,
        bool includingResolved = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.OldActivation)
            .Where(r => r.SubscriptionId == subscriptionId && !r.IsDeleted);

        if (!includingResolved)
        {
            query = query.Where(r => r.Status == ReplacementRequestStatus.Pending);
        }

        return await query
            .OrderByDescending(r => r.CreatedTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeviceReplacementRequest>> GetExpiredRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == ReplacementRequestStatus.Pending &&
                       r.ExpiresAtUtc <= DateTime.UtcNow &&
                       !r.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ExpireOldRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var expiredRequests = await GetExpiredRequestsAsync(cancellationToken);
        
        foreach (var request in expiredRequests)
        {
            request.Status = ReplacementRequestStatus.Expired;
            request.ResolvedAtUtc = DateTime.UtcNow;
        }

        return expiredRequests.Count;
    }

    public async Task<int> CountPendingByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(r => r.CompanyId == companyId && 
                            r.Status == ReplacementRequestStatus.Pending &&
                            r.ExpiresAtUtc > DateTime.UtcNow &&
                            !r.IsDeleted, 
                       cancellationToken);
    }
}

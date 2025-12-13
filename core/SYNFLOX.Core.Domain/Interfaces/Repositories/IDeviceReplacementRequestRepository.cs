using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for device replacement requests.
/// </summary>
public interface IDeviceReplacementRequestRepository : IBaseRepository<Guid, DeviceReplacementRequest>
{
    /// <summary>
    /// Get all pending requests for a company.
    /// </summary>
    Task<List<DeviceReplacementRequest>> GetPendingByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pending requests for a subscription.
    /// </summary>
    Task<List<DeviceReplacementRequest>> GetPendingBySubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get request by new machine hash (to check if already requested).
    /// </summary>
    Task<DeviceReplacementRequest?> GetPendingByMachineHashAsync(
        Guid subscriptionId,
        string machineHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all requests for a subscription (including resolved).
    /// </summary>
    Task<List<DeviceReplacementRequest>> GetBySubscriptionAsync(
        Guid subscriptionId,
        bool includingResolved = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired requests that need to be auto-rejected.
    /// </summary>
    Task<List<DeviceReplacementRequest>> GetExpiredRequestsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expire old pending requests.
    /// </summary>
    Task<int> ExpireOldRequestsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count pending requests for a company.
    /// </summary>
    Task<int> CountPendingByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}

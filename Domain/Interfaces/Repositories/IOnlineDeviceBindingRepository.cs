using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.OnlineAccess;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for OnlineDeviceBinding entity operations.
/// </summary>
public interface IOnlineDeviceBindingRepository : IBaseRepository<Guid, OnlineDeviceBinding>
{
    /// <summary>
    /// Get device by fingerprint for a subscription.
    /// </summary>
    Task<OnlineDeviceBinding?> GetByFingerprintAsync(Guid subscriptionId, string fingerprint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all devices for a subscription.
    /// </summary>
    Task<IEnumerable<OnlineDeviceBinding>> GetBySubscriptionIdAsync(Guid subscriptionId, bool includeInactive = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all devices for a token.
    /// </summary>
    Task<IEnumerable<OnlineDeviceBinding>> GetByTokenIdAsync(Guid tokenId, bool includeInactive = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get active device count for a subscription.
    /// </summary>
    Task<int> GetActiveDeviceCountAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a device is registered.
    /// </summary>
    Task<bool> IsDeviceRegisteredAsync(Guid subscriptionId, string fingerprint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update last seen information for a device.
    /// </summary>
    Task UpdateLastSeenAsync(Guid deviceId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increment API call count for a device.
    /// </summary>
    Task IncrementApiCallCountAsync(Guid deviceId, CancellationToken cancellationToken = default);
}

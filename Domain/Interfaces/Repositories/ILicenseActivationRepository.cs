using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for license activation management.
/// Handles device tracking, activation limits, and concurrent usage detection.
/// </summary>
public interface ILicenseActivationRepository : IBaseRepository<Guid, LicenseActivation>
{
    /// <summary>
    /// Get all activations for a subscription
    /// </summary>
    Task<List<LicenseActivation>> GetBySubscriptionAsync(
        Guid subscriptionId, 
        bool includeDeactivated = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all activations for a company
    /// </summary>
    Task<List<LicenseActivation>> GetByCompanyAsync(
        Guid companyId, 
        bool includeDeactivated = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find activation by machine hash for a subscription
    /// </summary>
    Task<LicenseActivation?> GetByMachineHashAsync(
        Guid subscriptionId, 
        string machineHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a machine is already activated for a subscription
    /// </summary>
    Task<bool> IsMachineActivatedAsync(
        Guid subscriptionId, 
        string machineHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count active activations for a subscription
    /// </summary>
    Task<int> GetActiveActivationCountAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recently active devices (for concurrent usage detection)
    /// </summary>
    Task<List<LicenseActivation>> GetRecentlyActiveDevicesAsync(
        Guid subscriptionId,
        int withinMinutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update last seen timestamp for an activation
    /// </summary>
    Task UpdateLastSeenAsync(
        Guid activationId, 
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate all devices for a subscription (e.g., on license revocation)
    /// </summary>
    Task DeactivateAllForSubscriptionAsync(
        Guid subscriptionId, 
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate specific device
    /// </summary>
    Task DeactivateDeviceAsync(
        Guid activationId, 
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment hardware change counter for an activation
    /// </summary>
    Task IncrementHardwareChangeAsync(
        Guid activationId,
        CancellationToken cancellationToken = default);
}

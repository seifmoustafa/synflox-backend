using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Entities.OnlineAccess;

namespace Application.Services;

/// <summary>
/// Interface for writing to Master DB from client-api.
/// Used to ensure client writes (devices, tokens) are visible to admin immediately.
/// </summary>
public interface IMasterDbWriter
{
    /// <summary>
    /// Writes a license activation to Master DB.
    /// </summary>
    Task<LicenseActivation> WriteActivationAsync(LicenseActivation activation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a license activation in Master DB.
    /// </summary>
    Task UpdateActivationAsync(LicenseActivation activation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes/Deactivates a device in Master DB.
    /// </summary>
    Task DeactivateDeviceAsync(Guid activationId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Writes a device replacement request to Master DB.
    /// </summary>
    Task<DeviceReplacementRequest> WriteReplacementRequestAsync(DeviceReplacementRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a replacement request in Master DB.
    /// </summary>
    Task UpdateReplacementRequestAsync(DeviceReplacementRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Writes an online token to Master DB.
    /// </summary>
    Task<OnlineClientToken> WriteOnlineTokenAsync(OnlineClientToken token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an online token in Master DB.
    /// </summary>
    Task UpdateOnlineTokenAsync(OnlineClientToken token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Writes an online device binding to Master DB.
    /// </summary>
    Task<OnlineDeviceBinding> WriteOnlineDeviceAsync(OnlineDeviceBinding device, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an online device binding in Master DB.
    /// </summary>
    Task UpdateOnlineDeviceAsync(OnlineDeviceBinding device, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if Master DB writing is available (client-api has MasterDbConnection).
    /// </summary>
    bool IsAvailable { get; }
}

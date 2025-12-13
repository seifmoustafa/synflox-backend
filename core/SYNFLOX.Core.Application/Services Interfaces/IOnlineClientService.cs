using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.OnlineAccess;

namespace Application.Services;

/// <summary>
/// Service interface for Online Client operations.
/// Handles token management, device registration, and entitlement access for online clients.
/// </summary>
public interface IOnlineClientService
{
    #region Token Management
    
    /// <summary>
    /// Generate a new online client token for a subscription.
    /// </summary>
    Task<GenerateOnlineTokenResponse> GenerateTokenAsync(GenerateOnlineTokenRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate an online client token.
    /// </summary>
    Task<OnlineTokenValidationDto> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all tokens for a company.
    /// </summary>
    Task<IEnumerable<OnlineClientTokenDto>> GetTokensByCompanyAsync(Guid companyId, bool includeRevoked = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all tokens for a subscription.
    /// </summary>
    Task<IEnumerable<OnlineClientTokenDto>> GetTokensBySubscriptionAsync(Guid subscriptionId, bool includeRevoked = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revoke a token.
    /// </summary>
    Task<bool> RevokeTokenAsync(Guid tokenId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Regenerate a token (revoke old, create new with same settings).
    /// </summary>
    Task<GenerateOnlineTokenResponse> RegenerateTokenAsync(Guid tokenId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update token usage statistics (last used, usage count, IP, user agent).
    /// </summary>
    Task UpdateTokenUsageAsync(Guid tokenId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Device Management
    
    /// <summary>
    /// Register a new device for a subscription.
    /// </summary>
    Task<RegisterDeviceResponse> RegisterDeviceAsync(Guid subscriptionId, RegisterDeviceRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unregister a device by fingerprint.
    /// </summary>
    Task<bool> UnregisterDeviceAsync(Guid subscriptionId, string deviceFingerprint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unregister a device by ID.
    /// </summary>
    Task<bool> UnregisterDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all devices for a subscription.
    /// </summary>
    Task<IEnumerable<OnlineDeviceDto>> GetDevicesAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get device limit information.
    /// </summary>
    Task<DeviceLimitDto> GetDeviceLimitAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update device last seen information.
    /// </summary>
    Task UpdateDeviceActivityAsync(Guid subscriptionId, string deviceFingerprint, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Entitlements
    
    /// <summary>
    /// Get the entitlement matrix for a subscription.
    /// </summary>
    Task<EntitlementMatrixDto> GetEntitlementsAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the entitlement version for cache validation.
    /// </summary>
    Task<int> GetEntitlementVersionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get subscription status for online client.
    /// </summary>
    Task<OnlineSubscriptionStatusDto> GetSubscriptionStatusAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Pending Changes
    
    /// <summary>
    /// Get pending changes for a subscription.
    /// </summary>
    Task<PendingChangesDto> GetPendingChangesAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    #endregion
}

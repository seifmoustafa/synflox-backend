using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.OfflineLicense;

namespace Application.Services;

/// <summary>
/// Service interface for offline license key management.
/// Handles generation, validation, and management of encrypted license keys
/// for offline client applications.
/// </summary>
public interface IOfflineLicenseService
{
    #region Generation

    /// <summary>
    /// Generate a new offline license key for a subscription.
    /// Creates an encrypted, signed key containing full entitlement matrix.
    /// </summary>
    /// <param name="subscriptionId">The subscription to generate a license for</param>
    /// <param name="request">Generation options including machine fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated license key and metadata</returns>
    Task<GenerateLicenseResponse> GenerateLicenseKeyAsync(
        Guid subscriptionId, 
        GenerateLicenseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerate license key for a subscription (after renewal, upgrade, or entitlement change).
    /// Invalidates the previous key and creates a new one.
    /// </summary>
    /// <param name="subscriptionId">The subscription to regenerate license for</param>
    /// <param name="request">Regeneration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New license key and metadata</returns>
    Task<GenerateLicenseResponse> RegenerateLicenseKeyAsync(
        Guid subscriptionId,
        GenerateLicenseRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Validate an offline license key.
    /// Decrypts the key, verifies signature, checks expiry, and returns entitlements.
    /// </summary>
    /// <param name="request">Validation request with license key and optional fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with entitlements</returns>
    Task<ValidateLicenseResponse> ValidateLicenseKeyAsync(
        ValidateLicenseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick validation check without returning full entitlements.
    /// Use for lightweight validation checks.
    /// </summary>
    /// <param name="licenseKey">The license key to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key is valid, false otherwise</returns>
    Task<bool> IsLicenseKeyValidAsync(
        string licenseKey,
        CancellationToken cancellationToken = default);

    #endregion

    #region Management

    /// <summary>
    /// Revoke a license key (makes it invalid immediately).
    /// Used when subscription is cancelled or suspended.
    /// </summary>
    /// <param name="subscriptionId">The subscription to revoke license for</param>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<bool> RevokeLicenseKeyAsync(
        Guid subscriptionId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get license information for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>License information</returns>
    Task<OfflineLicenseDto?> GetLicenseInfoAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all licenses for a company.
    /// </summary>
    /// <param name="companyId">The company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company license summary</returns>
    Task<CompanyLicenseSummaryDto> GetCompanyLicensesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a subscription has a valid (non-expired, non-revoked) license key.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if has valid key</returns>
    Task<bool> HasValidLicenseKeyAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Machine Management

    /// <summary>
    /// Add an additional authorized machine to a license.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="fingerprint">Machine fingerprint to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated license response</returns>
    Task<GenerateLicenseResponse> AddAuthorizedMachineAsync(
        Guid subscriptionId,
        MachineFingerprint fingerprint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove an authorized machine from a license.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="fingerprintHash">Hash of the fingerprint to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<bool> RemoveAuthorizedMachineAsync(
        Guid subscriptionId,
        string fingerprintHash,
        CancellationToken cancellationToken = default);

    #endregion

    #region Device Activation

    /// <summary>
    /// Activate a device for a subscription license.
    /// Checks device limits, concurrent usage, and hardware changes.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="request">Activation request with fingerprint</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Activation result</returns>
    Task<ActivationResponse> ActivateDeviceAsync(
        Guid subscriptionId,
        ActivateDeviceRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate a device from a subscription license.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="request">Deactivation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    Task<bool> DeactivateDeviceAsync(
        Guid subscriptionId,
        DeactivateDeviceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get activation summary for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Activation summary with device list</returns>
    Task<ActivationSummaryDto> GetActivationSummaryAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a heartbeat/validation from a device (updates LastSeenAt).
    /// Used for concurrent usage detection.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="machineHash">Machine fingerprint hash</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordDeviceHeartbeatAsync(
        Guid subscriptionId,
        string machineHash,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate all devices for a subscription.
    /// Used when license is revoked.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="reason">Reason for deactivation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeactivateAllDevicesAsync(
        Guid subscriptionId,
        string reason,
        CancellationToken cancellationToken = default);

    #endregion

    #region Utilities

    /// <summary>
    /// Compute machine fingerprint hash from raw fingerprint data.
    /// </summary>
    /// <param name="fingerprint">Raw fingerprint data</param>
    /// <returns>SHA256 hash of fingerprint</returns>
    string ComputeFingerprintHash(MachineFingerprint fingerprint);

    /// <summary>
    /// Get subscriptions with licenses expiring within specified days.
    /// Used by background job for sending reminders.
    /// </summary>
    /// <param name="days">Days until expiry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of subscription IDs with expiring licenses</returns>
    Task<IEnumerable<Guid>> GetExpiringLicensesAsync(
        int days,
        CancellationToken cancellationToken = default);

    #endregion
}

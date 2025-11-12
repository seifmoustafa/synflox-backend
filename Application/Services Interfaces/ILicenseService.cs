using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;

namespace Application.Services;

/// <summary>
/// Service for managing offline license keys tied to subscriptions
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Generate offline license key for a specific subscription
    /// Contains encrypted subscription data for offline validation
    /// </summary>
    /// <param name="subscriptionId">The subscription to generate key for</param>
    /// <returns>Generated license key string</returns>
    Task<GenerateLicenseKeyResponse> GenerateOfflineLicenseKeyAsync(Guid subscriptionId);

    /// <summary>
    /// Validate offline license key (used by client applications)
    /// Verifies signature, expiry, and subscription status
    /// </summary>
    /// <param name="request">License key validation request</param>
    /// <returns>Validation result with subscription details</returns>
    Task<LicenseKeyValidationResponse> ValidateOfflineLicenseKeyAsync(ValidateLicenseKeyRequest request);

    /// <summary>
    /// Regenerate license key for a subscription (when subscription changes)
    /// Invalidates old key and creates new one
    /// </summary>
    /// <param name="subscriptionId">The subscription to regenerate key for</param>
    /// <returns>New license key string</returns>
    Task<GenerateLicenseKeyResponse> RegenerateOfflineLicenseKeyAsync(Guid subscriptionId);

    /// <summary>
    /// Get all active license keys for a company (across all subscriptions)
    /// </summary>
    /// <param name="companyId">The company to get keys for</param>
    /// <returns>List of license keys with subscription details</returns>
    Task<IEnumerable<CompanyLicenseKeyDto>> GetCompanyLicenseKeysAsync(Guid companyId);

    /// <summary>
    /// Revoke/invalidate a license key (when subscription is cancelled/expired)
    /// </summary>
    /// <param name="subscriptionId">The subscription to revoke key for</param>
    /// <returns>Success status</returns>
    Task<bool> RevokeLicenseKeyAsync(Guid subscriptionId);

    /// <summary>
    /// Check if a subscription has a valid offline license key
    /// </summary>
    /// <param name="subscriptionId">The subscription to check</param>
    /// <returns>True if has valid key</returns>
    Task<bool> HasValidLicenseKeyAsync(Guid subscriptionId);
}

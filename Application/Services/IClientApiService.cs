using System;
using System.Threading.Tasks;
using Application.DTOs.ClientAccess;
using Application.DTOs.Licensing;

namespace Application.Services;

/// <summary>
/// Service interface for client-facing API operations
/// These are the services that external clients can access with their tokens
/// </summary>
public interface IClientApiService
{
    /// <summary>
    /// Gets subscription status for the authenticated client
    /// </summary>
    Task<ClientSubscriptionStatusDto> GetSubscriptionStatusAsync(Guid companyId, Guid subscriptionId);

    /// <summary>
    /// Validates a license key for the authenticated client
    /// </summary>
    Task<LicenseKeyValidationResponse> ValidateLicenseKeyAsync(string licenseKey, Guid companyId);

    /// <summary>
    /// Gets company profile information that client is allowed to see
    /// </summary>
    Task<ClientCompanyProfileDto> GetCompanyProfileAsync(Guid companyId);

    /// <summary>
    /// Gets usage statistics for the client's token
    /// </summary>
    Task<ClientTokenUsageDto> GetTokenUsageStatisticsAsync(Guid tokenId, int days = 30);

    /// <summary>
    /// Gets plan features and capabilities for the subscription
    /// </summary>
    Task<ClientPlanFeaturesDto> GetPlanFeaturesAsync(Guid subscriptionId);

    /// <summary>
    /// Health check endpoint for client systems
    /// </summary>
    Task<ClientHealthCheckDto> GetHealthCheckAsync(Guid companyId);

    /// <summary>
    /// Gets subscription history for the company (limited view)
    /// </summary>
    Task<ClientSubscriptionHistoryDto> GetSubscriptionHistoryAsync(Guid companyId, int months = 12);

    /// <summary>
    /// Validates token and returns basic token info
    /// </summary>
    Task<ClientTokenValidationDto> ValidateTokenAsync(string token);
}

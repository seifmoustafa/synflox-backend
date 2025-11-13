using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.ClientAccess;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Service interface for managing client access tokens
/// </summary>
public interface IClientTokenService
{
    /// <summary>
    /// Generates a new client access token for a subscription
    /// </summary>
    Task<GenerateClientTokenResponse> GenerateTokenAsync(GenerateClientTokenRequest request);

    /// <summary>
    /// Validates a client token and returns token information
    /// </summary>
    Task<ClientTokenDto?> ValidateTokenAsync(string token);

    /// <summary>
    /// Gets all tokens for a company
    /// </summary>
    Task<IEnumerable<ClientTokenDto>> GetCompanyTokensAsync(Guid companyId);

    /// <summary>
    /// Gets all tokens for a subscription
    /// </summary>
    Task<IEnumerable<ClientTokenDto>> GetSubscriptionTokensAsync(Guid subscriptionId);

    /// <summary>
    /// Revokes a specific token
    /// </summary>
    Task<bool> RevokeTokenAsync(Guid tokenId, string reason);

    /// <summary>
    /// Revokes all tokens for a subscription
    /// </summary>
    Task<int> RevokeAllSubscriptionTokensAsync(Guid subscriptionId, string reason);

    /// <summary>
    /// Revokes all tokens for a company
    /// </summary>
    Task<int> RevokeAllCompanyTokensAsync(Guid companyId, string reason);

    /// <summary>
    /// Regenerates token for a subscription (revokes old, creates new)
    /// </summary>
    Task<GenerateClientTokenResponse> RegenerateTokenAsync(Guid subscriptionId, string reason);

    /// <summary>
    /// Gets token usage statistics
    /// </summary>
    Task<ClientTokenUsageDto> GetTokenUsageAsync(Guid tokenId, int days = 30);

    /// <summary>
    /// Records token usage for analytics
    /// </summary>
    Task RecordTokenUsageAsync(Guid tokenId, string endpoint, string httpMethod, 
        int statusCode, long responseTimeMs, string? clientIp, string? userAgent, string? errorMessage = null);

    /// <summary>
    /// Checks if a token has access to a specific endpoint
    /// </summary>
    Task<bool> HasEndpointAccessAsync(string token, string endpoint);

    /// <summary>
    /// Gets tokens expiring within specified days
    /// </summary>
    Task<IEnumerable<ClientTokenDto>> GetExpiringTokensAsync(int daysFromNow);

    /// <summary>
    /// Cleans up expired tokens
    /// </summary>
    Task<int> CleanupExpiredTokensAsync(int olderThanDays = 30);

    /// <summary>
    /// Updates token status based on subscription changes
    /// </summary>
    Task UpdateTokensForSubscriptionStatusAsync(Guid subscriptionId, bool isActive, string? reason = null);

    /// <summary>
    /// Gets comprehensive token analytics for admin dashboard
    /// </summary>
    Task<Dictionary<string, object>> GetTokenAnalyticsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Automatically generates token when subscription is created
    /// </summary>
    Task<GenerateClientTokenResponse> AutoGenerateTokenForSubscriptionAsync(Guid subscriptionId);
}

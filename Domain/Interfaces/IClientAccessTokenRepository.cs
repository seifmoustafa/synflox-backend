using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.ClientAccess;
using Domain.Enums;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for ClientAccessToken operations
/// </summary>
public interface IClientAccessTokenRepository : IBaseRepository<Guid, ClientAccessToken>
{
    /// <summary>
    /// Gets a client token by its hash
    /// </summary>
    Task<ClientAccessToken?> GetByTokenHashAsync(string tokenHash);

    /// <summary>
    /// Gets all active tokens for a company
    /// </summary>
    Task<IEnumerable<ClientAccessToken>> GetActiveTokensByCompanyIdAsync(Guid companyId);

    /// <summary>
    /// Gets all tokens for a subscription
    /// </summary>
    Task<IEnumerable<ClientAccessToken>> GetTokensBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Gets tokens that are expiring within the specified days
    /// </summary>
    Task<IEnumerable<ClientAccessToken>> GetExpiringTokensAsync(int daysFromNow);

    /// <summary>
    /// Revokes all tokens for a subscription
    /// </summary>
    Task RevokeAllTokensForSubscriptionAsync(Guid subscriptionId, string reason);

    /// <summary>
    /// Revokes all tokens for a company
    /// </summary>
    Task RevokeAllTokensForCompanyAsync(Guid companyId, string reason);

    /// <summary>
    /// Updates token usage statistics
    /// </summary>
    Task UpdateTokenUsageAsync(Guid tokenId, string? clientIp, string? userAgent);

    /// <summary>
    /// Gets token usage statistics for a date range
    /// </summary>
    Task<Dictionary<string, int>> GetUsageStatisticsAsync(Guid tokenId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Cleans up expired tokens older than specified days
    /// </summary>
    Task CleanupExpiredTokensAsync(int olderThanDays);

    /// <summary>
    /// Gets tokens by status
    /// </summary>
    Task<IEnumerable<ClientAccessToken>> GetTokensByStatusAsync(ClientTokenStatus status);

    /// <summary>
    /// Checks if a company has any active tokens
    /// </summary>
    Task<bool> HasActiveTokensAsync(Guid companyId);
}

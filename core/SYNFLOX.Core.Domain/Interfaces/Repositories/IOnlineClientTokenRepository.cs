using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.OnlineAccess;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for OnlineClientToken entity operations.
/// </summary>
public interface IOnlineClientTokenRepository : IBaseRepository<Guid, OnlineClientToken>
{
    /// <summary>
    /// Get token by its hash.
    /// </summary>
    Task<OnlineClientToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all tokens for a company.
    /// </summary>
    Task<IEnumerable<OnlineClientToken>> GetByCompanyIdAsync(Guid companyId, bool includeRevoked = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all tokens for a subscription.
    /// </summary>
    Task<IEnumerable<OnlineClientToken>> GetBySubscriptionIdAsync(Guid subscriptionId, bool includeRevoked = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get token with all bound devices.
    /// </summary>
    Task<OnlineClientToken?> GetWithDevicesAsync(Guid tokenId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get active token for a subscription.
    /// </summary>
    Task<OnlineClientToken?> GetActiveTokenForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update last used information.
    /// </summary>
    Task UpdateLastUsedAsync(Guid tokenId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get tokens expiring within specified days.
    /// </summary>
    Task<IEnumerable<OnlineClientToken>> GetExpiringTokensAsync(int daysFromNow, CancellationToken cancellationToken = default);
}

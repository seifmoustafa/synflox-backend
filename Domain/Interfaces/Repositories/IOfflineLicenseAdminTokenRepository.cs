using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing offline license admin tokens.
/// </summary>
public interface IOfflineLicenseAdminTokenRepository : IBaseRepository<Guid, OfflineLicenseAdminToken>
{
    /// <summary>
    /// Get all tokens for a company
    /// </summary>
    Task<List<OfflineLicenseAdminToken>> GetByCompanyAsync(
        Guid companyId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get token by its hash
    /// </summary>
    Task<OfflineLicenseAdminToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active token for a company
    /// </summary>
    Task<OfflineLicenseAdminToken?> GetActiveTokenForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if company has any active tokens
    /// </summary>
    Task<bool> HasActiveTokenAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all tokens for a company
    /// </summary>
    Task<int> RevokeAllForCompanyAsync(
        Guid companyId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired tokens that need cleanup
    /// </summary>
    Task<List<OfflineLicenseAdminToken>> GetExpiredTokensAsync(
        int daysExpired = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update token usage statistics
    /// </summary>
    Task UpdateUsageAsync(
        Guid tokenId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}

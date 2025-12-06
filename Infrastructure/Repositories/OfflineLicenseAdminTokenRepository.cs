using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for offline license admin token management.
/// </summary>
public class OfflineLicenseAdminTokenRepository : BaseRepository<Guid, OfflineLicenseAdminToken>, IOfflineLicenseAdminTokenRepository
{
    public OfflineLicenseAdminTokenRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<List<OfflineLicenseAdminToken>> GetByCompanyAsync(
        Guid companyId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.CompanyId == companyId && !t.IsDeleted);
        
        if (!includeRevoked)
        {
            query = query.Where(t => t.Status == ClientTokenStatus.Active);
        }

        return await query
            .OrderByDescending(t => t.IssuedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<OfflineLicenseAdminToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Company)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsDeleted, cancellationToken);
    }

    public async Task<OfflineLicenseAdminToken?> GetActiveTokenForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.CompanyId == companyId && 
                       t.Status == ClientTokenStatus.Active && 
                       t.ExpiresAtUtc > DateTime.UtcNow && 
                       !t.IsDeleted)
            .OrderByDescending(t => t.IssuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasActiveTokenAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(t => t.CompanyId == companyId && 
                          t.Status == ClientTokenStatus.Active && 
                          t.ExpiresAtUtc > DateTime.UtcNow && 
                          !t.IsDeleted, 
                     cancellationToken);
    }

    public async Task<int> RevokeAllForCompanyAsync(
        Guid companyId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _dbSet
            .Where(t => t.CompanyId == companyId && 
                       t.Status == ClientTokenStatus.Active && 
                       !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.Status = ClientTokenStatus.Revoked;
            token.RevocationReason = reason;
            token.RevokedAtUtc = now;
        }

        return tokens.Count;
    }

    public async Task<List<OfflineLicenseAdminToken>> GetExpiredTokensAsync(
        int daysExpired = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysExpired);
        
        return await _dbSet
            .Where(t => t.ExpiresAtUtc < cutoffDate && !t.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateUsageAsync(
        Guid tokenId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbSet.FindAsync(new object[] { tokenId }, cancellationToken);
        if (token != null)
        {
            token.RecordApiCall(ipAddress, userAgent);
        }
    }
}

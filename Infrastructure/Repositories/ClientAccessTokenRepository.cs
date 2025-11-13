using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.ClientAccess;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ClientAccessToken operations
/// </summary>
public class ClientAccessTokenRepository : BaseRepository<Guid, ClientAccessToken>, IClientAccessTokenRepository
{
    public ClientAccessTokenRepository(ApplicationDBContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a client token by its hash with related data
    /// </summary>
    public async Task<ClientAccessToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _dbSet
            .Include(t => t.Company)
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsDeleted);
    }

    /// <summary>
    /// Gets all active tokens for a company
    /// </summary>
    public async Task<IEnumerable<ClientAccessToken>> GetActiveTokensByCompanyIdAsync(Guid companyId)
    {
        return await _dbSet
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(t => t.CompanyId == companyId && 
                       t.Status == ClientTokenStatus.Active && 
                       t.ExpiresAtUtc > DateTime.UtcNow && 
                       !t.IsDeleted)
            .OrderByDescending(t => t.IssuedAtUtc)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all tokens for a subscription
    /// </summary>
    public async Task<IEnumerable<ClientAccessToken>> GetTokensBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(t => t.Company)
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(t => t.SubscriptionId == subscriptionId && !t.IsDeleted)
            .OrderByDescending(t => t.IssuedAtUtc)
            .ToListAsync();
    }

    /// <summary>
    /// Gets tokens that are expiring within the specified days
    /// </summary>
    public async Task<IEnumerable<ClientAccessToken>> GetExpiringTokensAsync(int daysFromNow)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysFromNow);
        
        return await _dbSet
            .Include(t => t.Company)
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(t => t.Status == ClientTokenStatus.Active && 
                       t.ExpiresAtUtc <= expiryThreshold && 
                       t.ExpiresAtUtc > DateTime.UtcNow && 
                       !t.IsDeleted)
            .OrderBy(t => t.ExpiresAtUtc)
            .ToListAsync();
    }

    /// <summary>
    /// Revokes all tokens for a subscription
    /// </summary>
    public async Task RevokeAllTokensForSubscriptionAsync(Guid subscriptionId, string reason)
    {
        var tokens = await _dbSet
            .Where(t => t.SubscriptionId == subscriptionId && 
                       t.Status == ClientTokenStatus.Active && 
                       !t.IsDeleted)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Status = ClientTokenStatus.Revoked;
            token.RevocationReason = reason;
            token.UpdatedTimestamp = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Revokes all tokens for a company
    /// </summary>
    public async Task RevokeAllTokensForCompanyAsync(Guid companyId, string reason)
    {
        var tokens = await _dbSet
            .Where(t => t.CompanyId == companyId && 
                       t.Status == ClientTokenStatus.Active && 
                       !t.IsDeleted)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Status = ClientTokenStatus.Revoked;
            token.RevocationReason = reason;
            token.UpdatedTimestamp = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates token usage statistics
    /// </summary>
    public async Task UpdateTokenUsageAsync(Guid tokenId, string? clientIp, string? userAgent)
    {
        var token = await _dbSet.FindAsync(tokenId);
        if (token != null)
        {
            token.LastUsedAtUtc = DateTime.UtcNow;
            token.UsageCount++;
            token.LastUsedFromIp = clientIp;
            token.LastUsedUserAgent = userAgent;
            token.UpdatedTimestamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets token usage statistics for a date range
    /// </summary>
    public async Task<Dictionary<string, int>> GetUsageStatisticsAsync(Guid tokenId, DateTime fromDate, DateTime toDate)
    {
        var usageLogs = await _context.Set<ClientTokenUsageLog>()
            .Where(log => log.ClientTokenId == tokenId && 
                         log.RequestTimestampUtc >= fromDate && 
                         log.RequestTimestampUtc <= toDate)
            .ToListAsync();

        return new Dictionary<string, int>
        {
            ["TotalRequests"] = usageLogs.Count,
            ["SuccessfulRequests"] = usageLogs.Count(log => log.ResponseStatusCode >= 200 && log.ResponseStatusCode < 300),
            ["FailedRequests"] = usageLogs.Count(log => log.ResponseStatusCode < 200 || log.ResponseStatusCode >= 300),
            ["UniqueEndpoints"] = usageLogs.Select(log => log.Endpoint).Distinct().Count(),
            ["UniqueIPs"] = usageLogs.Where(log => !string.IsNullOrEmpty(log.ClientIpAddress))
                                   .Select(log => log.ClientIpAddress).Distinct().Count()
        };
    }

    /// <summary>
    /// Cleans up expired tokens older than specified days
    /// </summary>
    public async Task CleanupExpiredTokensAsync(int olderThanDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        
        var expiredTokens = await _dbSet
            .Where(t => t.ExpiresAtUtc < cutoffDate || 
                       (t.Status == ClientTokenStatus.Expired && t.UpdatedTimestamp < cutoffDate))
            .ToListAsync();

        foreach (var token in expiredTokens)
        {
            token.IsDeleted = true;
            token.UpdatedTimestamp = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets tokens by status
    /// </summary>
    public async Task<IEnumerable<ClientAccessToken>> GetTokensByStatusAsync(ClientTokenStatus status)
    {
        return await _dbSet
            .Include(t => t.Company)
            .Include(t => t.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(t => t.Status == status && !t.IsDeleted)
            .OrderByDescending(t => t.IssuedAtUtc)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a company has any active tokens
    /// </summary>
    public async Task<bool> HasActiveTokensAsync(Guid companyId)
    {
        return await _dbSet
            .AnyAsync(t => t.CompanyId == companyId && 
                          t.Status == ClientTokenStatus.Active && 
                          t.ExpiresAtUtc > DateTime.UtcNow && 
                          !t.IsDeleted);
    }
}

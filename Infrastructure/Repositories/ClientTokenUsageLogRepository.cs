using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.ClientAccess;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ClientTokenUsageLog operations
/// </summary>
public class ClientTokenUsageLogRepository : BaseRepository<Guid, ClientTokenUsageLog>, IClientTokenUsageLogRepository
{
    public ClientTokenUsageLogRepository(ApplicationDBContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets usage logs for a specific token with pagination
    /// </summary>
    public async Task<IEnumerable<ClientTokenUsageLog>> GetUsageLogsByTokenIdAsync(Guid tokenId, int page = 1, int pageSize = 50)
    {
        return await _dbSet
            .Where(log => log.ClientTokenId == tokenId)
            .OrderByDescending(log => log.RequestTimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Gets usage logs for a date range
    /// </summary>
    public async Task<IEnumerable<ClientTokenUsageLog>> GetUsageLogsByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _dbSet
            .Where(log => log.RequestTimestampUtc >= fromDate && log.RequestTimestampUtc <= toDate)
            .OrderByDescending(log => log.RequestTimestampUtc)
            .ToListAsync();
    }

    /// <summary>
    /// Gets usage statistics by endpoint for a specific token
    /// </summary>
    public async Task<Dictionary<string, int>> GetEndpointUsageStatisticsAsync(Guid tokenId, DateTime fromDate, DateTime toDate)
    {
        var endpointUsage = await _dbSet
            .Where(log => log.ClientTokenId == tokenId && 
                         log.RequestTimestampUtc >= fromDate && 
                         log.RequestTimestampUtc <= toDate)
            .GroupBy(log => log.Endpoint)
            .Select(group => new { Endpoint = group.Key, Count = group.Count() })
            .ToListAsync();

        return endpointUsage.ToDictionary(x => x.Endpoint, x => x.Count);
    }

    /// <summary>
    /// Gets failed requests for a token in a date range
    /// </summary>
    public async Task<IEnumerable<ClientTokenUsageLog>> GetFailedRequestsAsync(Guid tokenId, DateTime fromDate, DateTime toDate)
    {
        return await _dbSet
            .Where(log => log.ClientTokenId == tokenId && 
                         log.RequestTimestampUtc >= fromDate && 
                         log.RequestTimestampUtc <= toDate &&
                         (log.ResponseStatusCode < 200 || log.ResponseStatusCode >= 300))
            .OrderByDescending(log => log.RequestTimestampUtc)
            .ToListAsync();
    }

    /// <summary>
    /// Gets usage count for a token in a time period
    /// </summary>
    public async Task<int> GetUsageCountAsync(Guid tokenId, DateTime fromDate, DateTime toDate)
    {
        return await _dbSet
            .CountAsync(log => log.ClientTokenId == tokenId && 
                              log.RequestTimestampUtc >= fromDate && 
                              log.RequestTimestampUtc <= toDate);
    }

    /// <summary>
    /// Gets average response time for a token
    /// </summary>
    public async Task<double> GetAverageResponseTimeAsync(Guid tokenId, DateTime fromDate, DateTime toDate)
    {
        var logs = await _dbSet
            .Where(log => log.ClientTokenId == tokenId && 
                         log.RequestTimestampUtc >= fromDate && 
                         log.RequestTimestampUtc <= toDate &&
                         log.ResponseStatusCode >= 200 && log.ResponseStatusCode < 300)
            .Select(log => log.ResponseTimeMs)
            .ToListAsync();

        return logs.Any() ? logs.Average() : 0;
    }

    /// <summary>
    /// Cleans up old usage logs
    /// </summary>
    public async Task CleanupOldLogsAsync(int olderThanDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        
        var oldLogs = await _dbSet
            .Where(log => log.RequestTimestampUtc < cutoffDate)
            .ToListAsync();

        _dbSet.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets top used endpoints across all tokens
    /// </summary>
    public async Task<Dictionary<string, int>> GetTopEndpointsAsync(DateTime fromDate, DateTime toDate, int topCount = 10)
    {
        var topEndpoints = await _dbSet
            .Where(log => log.RequestTimestampUtc >= fromDate && log.RequestTimestampUtc <= toDate)
            .GroupBy(log => log.Endpoint)
            .Select(group => new { Endpoint = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topCount)
            .ToListAsync();

        return topEndpoints.ToDictionary(x => x.Endpoint, x => x.Count);
    }

    /// <summary>
    /// Gets usage logs by IP address for security analysis
    /// </summary>
    public async Task<IEnumerable<ClientTokenUsageLog>> GetUsageLogsByIpAddressAsync(string ipAddress, DateTime fromDate, DateTime toDate)
    {
        return await _dbSet
            .Where(log => log.ClientIpAddress == ipAddress && 
                         log.RequestTimestampUtc >= fromDate && 
                         log.RequestTimestampUtc <= toDate)
            .OrderByDescending(log => log.RequestTimestampUtc)
            .ToListAsync();
    }
}

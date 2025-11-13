using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.ClientAccess;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for ClientTokenUsageLog operations
/// </summary>
public interface IClientTokenUsageLogRepository : IBaseRepository<Guid, ClientTokenUsageLog>
{
    /// <summary>
    /// Gets usage logs for a specific token
    /// </summary>
    Task<IEnumerable<ClientTokenUsageLog>> GetUsageLogsByTokenIdAsync(Guid tokenId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Gets usage logs for a date range
    /// </summary>
    Task<IEnumerable<ClientTokenUsageLog>> GetUsageLogsByDateRangeAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets usage statistics by endpoint
    /// </summary>
    Task<Dictionary<string, int>> GetEndpointUsageStatisticsAsync(Guid tokenId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets failed requests for a token
    /// </summary>
    Task<IEnumerable<ClientTokenUsageLog>> GetFailedRequestsAsync(Guid tokenId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets usage count for a token in a time period
    /// </summary>
    Task<int> GetUsageCountAsync(Guid tokenId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets average response time for a token
    /// </summary>
    Task<double> GetAverageResponseTimeAsync(Guid tokenId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Cleans up old usage logs
    /// </summary>
    Task CleanupOldLogsAsync(int olderThanDays);

    /// <summary>
    /// Gets top used endpoints across all tokens
    /// </summary>
    Task<Dictionary<string, int>> GetTopEndpointsAsync(DateTime fromDate, DateTime toDate, int topCount = 10);

    /// <summary>
    /// Gets usage logs by IP address for security analysis
    /// </summary>
    Task<IEnumerable<ClientTokenUsageLog>> GetUsageLogsByIpAddressAsync(string ipAddress, DateTime fromDate, DateTime toDate);
}

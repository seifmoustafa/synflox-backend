using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Metrics;
using Domain.Enums;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for SystemMetric entities.
/// </summary>
public interface ISystemMetricRepository : IBaseRepository<Guid, SystemMetric>
{
    /// <summary>
    /// Gets metrics by type within a date range.
    /// </summary>
    Task<IEnumerable<SystemMetric>> GetByTypeAsync(
        MetricType metricType,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets the latest metric of a specific type.
    /// </summary>
    Task<SystemMetric?> GetLatestByTypeAsync(MetricType metricType);

    /// <summary>
    /// Gets aggregated metrics (hourly, daily, monthly).
    /// </summary>
    Task<IEnumerable<SystemMetric>> GetAggregatedAsync(
        MetricType metricType,
        string aggregationPeriod,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Deletes metrics older than the specified date.
    /// </summary>
    Task<int> DeleteOldMetricsAsync(DateTime beforeDate);
}




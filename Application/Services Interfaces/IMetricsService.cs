using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Metrics;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Service interface for system metrics and telemetry.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Records a metric value.
    /// </summary>
    Task RecordMetricAsync(MetricType metricType, double value, string? tags = null);

    /// <summary>
    /// Gets real-time metric summary.
    /// </summary>
    Task<MetricSummaryDto> GetSummaryAsync();

    /// <summary>
    /// Gets historical metrics for a specific type.
    /// </summary>
    Task<MetricHistoryDto> GetHistoryAsync(
        MetricType metricType,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? aggregationPeriod = null);

    /// <summary>
    /// Aggregates metrics (hourly, daily, monthly).
    /// </summary>
    Task AggregateMetricsAsync();

    /// <summary>
    /// Cleans up old metrics (keeps only last N days of raw data).
    /// </summary>
    Task CleanupOldMetricsAsync(int keepDays = 30);
}




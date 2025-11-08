using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Metrics;

/// <summary>
/// Represents a system metric measurement at a specific point in time.
/// </summary>
public class SystemMetric : BaseEntity<Guid>
{
    /// <summary>
    /// The type of metric being measured.
    /// </summary>
    [Required]
    public MetricType MetricType { get; set; }

    /// <summary>
    /// The numeric value of the metric.
    /// </summary>
    [Required]
    public double Value { get; set; }

    /// <summary>
    /// Timestamp when the metric was recorded.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional JSON object containing additional tags/metadata for the metric.
    /// Example: {"endpoint": "/api/companies", "method": "GET", "statusCode": 200}
    /// </summary>
    [StringLength(2000)]
    public string? Tags { get; set; }

    /// <summary>
    /// Optional aggregation period (e.g., "hourly", "daily", "monthly").
    /// Null for raw metrics.
    /// </summary>
    [StringLength(50)]
    public string? AggregationPeriod { get; set; }
}




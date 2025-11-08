using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Metrics;

/// <summary>
/// DTO for historical metric data.
/// </summary>
public class MetricHistoryDto
{
    public MetricType MetricType { get; set; }
    public string MetricTypeName { get; set; } = string.Empty;
    public List<MetricDataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// Represents a single data point in a metric history.
/// </summary>
public class MetricDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string? Tags { get; set; }
}




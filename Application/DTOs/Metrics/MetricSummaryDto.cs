using System;
using System.Collections.Generic;

namespace Application.DTOs.Metrics;

/// <summary>
/// DTO for real-time metric summary.
/// </summary>
public class MetricSummaryDto
{
    public Dictionary<string, double> CurrentMetrics { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}




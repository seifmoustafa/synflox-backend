using System;
using System.Collections.Generic;

namespace Application.DTOs.Analytics;

/// <summary>
/// DTO for company usage analytics.
/// </summary>
public class CompanyUsageAnalyticsDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public Dictionary<string, int> RequestsByEndpoint { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> RequestsByMethod { get; set; } = new Dictionary<string, int>();
    public double AverageResponseTimeMs { get; set; }
    public DateTime? LastRequestTime { get; set; }
    public Dictionary<DateTime, int> RequestsByDate { get; set; } = new Dictionary<DateTime, int>();
    public Dictionary<int, int> RequestsByStatusCode { get; set; } = new Dictionary<int, int>();
}


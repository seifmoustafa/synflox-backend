namespace Application.DTOs.Subscriptions;

/// <summary>
/// Comprehensive subscription analytics data
/// </summary>
public class SubscriptionAnalyticsDto
{
    public Guid SubscriptionId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public AnalyticsPeriodDto Period { get; set; } = new();
    public SubscriptionStatusAnalyticsDto Status { get; set; } = new();
    public UsageAnalyticsDto Usage { get; set; } = new();
    public List<MonthlyUsageDto> MonthlyTrends { get; set; } = new();
    public List<StatusDistributionDto> StatusDistribution { get; set; } = new();
    public List<FeatureUsageDto> FeatureUsage { get; set; } = new();
    public PerformanceMetricsDto Performance { get; set; } = new();
    public List<SubscriptionHistoryItemDto> RecentHistory { get; set; } = new();
}

public class AnalyticsPeriodDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class SubscriptionStatusAnalyticsDto
{
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsTrial { get; set; }
    public bool IsLifetime { get; set; }
    public int DaysRemaining { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
}

public class UsageAnalyticsDto
{
    public int TotalDays { get; set; }
    public int ActiveDays { get; set; }
    public double UtilizationPercentage { get; set; }
    public int TotalLogins { get; set; }
    public int UniqueUsers { get; set; }
    public int ApiCalls { get; set; }
}

public class MonthlyUsageDto
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public double Usage { get; set; }
    public decimal Revenue { get; set; }
    public int ActiveDays { get; set; }
}

public class StatusDistributionDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class FeatureUsageDto
{
    public string Feature { get; set; } = string.Empty;
    public int Usage { get; set; }
    public bool IsEnabled { get; set; }
}

public class PerformanceMetricsDto
{
    public double UptimePercentage { get; set; } = 99.9;
    public int ResponseTimeMs { get; set; } = 150;
    public double ErrorRate { get; set; } = 0.01;
    public double TicketResolutionRate { get; set; } = 98.5;
    public double AvgResponseTimeHours { get; set; } = 2.3;
    public double SatisfactionScore { get; set; } = 4.8;
}

public class SubscriptionHistoryItemDto
{
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
    public string? PerformedBy { get; set; }
}

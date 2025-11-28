namespace Application.DTOs.Dashboard.Revenue;

/// <summary>
/// Revenue trend data point
/// </summary>
public record RevenueTrendPointDto(
    DateTime Date,
    string Period,              // "Jan 2025", "Week 1", etc.
    decimal Revenue,
    decimal NewRevenue,         // From new subscriptions
    decimal RecurringRevenue,   // From existing subscriptions
    decimal ChurnedRevenue      // Lost from cancellations
);

/// <summary>
/// Revenue trend summary
/// </summary>
public record RevenueTrendDto(
    List<RevenueTrendPointDto> DataPoints,
    decimal TotalGrowth,
    decimal GrowthRate,
    string TrendDirection,      // "up", "down", "stable"
    decimal HighestRevenue,
    DateTime HighestRevenueDate,
    decimal LowestRevenue,
    DateTime LowestRevenueDate
);

namespace Application.DTOs.Dashboard.Subscriptions;

/// <summary>
/// Subscription growth data point
/// </summary>
public record SubscriptionGrowthPointDto(
    DateTime Date,
    int NewSubscriptions,
    int ActiveSubscriptions,
    int Churned
);

/// <summary>
/// Subscription growth summary
/// </summary>
public record SubscriptionGrowthSummaryDto(
    int NetGrowth,              // New - Churned
    decimal GrowthRate,
    int TotalNew,
    int TotalChurned,
    List<SubscriptionGrowthPointDto> DailyData
);

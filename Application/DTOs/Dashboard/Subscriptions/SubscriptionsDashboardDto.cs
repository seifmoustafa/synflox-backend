using Application.DTOs.Dashboard.Shared;

namespace Application.DTOs.Dashboard.Subscriptions;

/// <summary>
/// Subscriptions dashboard response with all subscription analytics
/// </summary>
public record SubscriptionsDashboardDto(
    // Summary Stats
    int TotalSubscriptions,
    int ActiveSubscriptions,
    int TrialSubscriptions,
    decimal TotalMonthlyRevenue,
    
    // Status Distribution
    SubscriptionStatusDistributionDto StatusDistribution,
    List<DistributionItemDto> StatusChart,
    
    // By Plan Analysis
    SubscriptionsByPlanSummaryDto ByPlan,
    
    // Expiry Timeline
    ExpiryTimelineDto ExpiryTimeline,
    
    // Lifecycle Metrics
    LifecycleMetricsDto LifecycleMetrics,
    
    // Growth Trend
    SubscriptionGrowthSummaryDto Growth,
    
    // Timestamp
    DateTime GeneratedAt
);

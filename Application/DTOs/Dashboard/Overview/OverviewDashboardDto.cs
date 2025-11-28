using Application.DTOs.Dashboard.Shared;

namespace Application.DTOs.Dashboard.Overview;

/// <summary>
/// Main overview dashboard response containing all summary data
/// </summary>
public record OverviewDashboardDto(
    // KPI Cards
    KpiCardDto CompaniesKpi,
    KpiCardDto SubscriptionsKpi,
    KpiCardDto RevenueKpi,
    KpiCardDto AlertsKpi,
    
    // Quick Stats
    QuickStatsDto Stats,
    
    // Mini Charts Data
    List<DistributionItemDto> SubscriptionStatusDistribution,
    List<TimeSeriesDataPointDto> GrowthTrend,  // Last 30 days
    
    // Recent Activity Feed
    List<RecentActivityItemDto> RecentActivity,
    
    // Timestamp
    DateTime GeneratedAt
);

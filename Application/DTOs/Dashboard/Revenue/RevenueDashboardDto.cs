using Application.DTOs.Dashboard.Shared;

namespace Application.DTOs.Dashboard.Revenue;

/// <summary>
/// Revenue dashboard response with all financial analytics
/// </summary>
public record RevenueDashboardDto(
    // Core Metrics
    RevenueMetricsDto Metrics,
    
    // Revenue by Plan
    RevenueByPlanDto ByPlan,
    List<DistributionItemDto> ByPlanChart,
    
    // Revenue Trend (last 12 months)
    RevenueTrendDto Trend,
    
    // Projections
    RevenueProjectionDto Projections,
    
    // Quick Stats
    decimal TotalLifetimeRevenue,
    decimal AverageOrderValue,
    int TotalTransactions,
    
    // Currency breakdown (if multi-currency)
    List<DistributionItemDto> ByCurrency,
    
    // Timestamp
    DateTime GeneratedAt
);

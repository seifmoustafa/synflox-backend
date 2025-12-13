using Application.DTOs.Dashboard.Shared;

namespace Application.DTOs.Dashboard.Revenue;

/// <summary>
/// Revenue dashboard response with all financial analytics
/// All monetary values are converted to the DisplayCurrency using real-time exchange rates
/// </summary>
public record RevenueDashboardDto(
    // Display currency info
    string DisplayCurrency,
    string DisplayCurrencySymbol,
    
    // Core Metrics (all amounts in DisplayCurrency)
    RevenueMetricsDto Metrics,
    
    // Revenue by Plan
    RevenueByPlanDto ByPlan,
    List<DistributionItemDto> ByPlanChart,
    
    // Revenue Trend (last 12 months)
    RevenueTrendDto Trend,
    
    // Projections
    RevenueProjectionDto Projections,
    
    // Quick Stats (all amounts in DisplayCurrency)
    decimal TotalLifetimeRevenue,
    decimal AverageOrderValue,
    int TotalTransactions,
    
    // Currency breakdown (original currencies before conversion)
    List<DistributionItemDto> ByCurrency,
    
    // Exchange rate info
    DateTime ExchangeRatesUpdatedAt,
    
    // Timestamp
    DateTime GeneratedAt
);

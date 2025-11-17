// ============================================================================
// SYNFLOX Dashboard DTOs - SOLID Compliant Architecture
// ============================================================================
// This file contains only the main DashboardDto aggregator.
// All sub-DTOs are organized in feature-based folders following SRP.
// ============================================================================

using Application.DTOs.Dashboard.Activity;
using Application.DTOs.Dashboard.Admins;
using Application.DTOs.Dashboard.Alerts;
using Application.DTOs.Dashboard.Companies;
using Application.DTOs.Dashboard.Lifecycle;
using Application.DTOs.Dashboard.Overview;
using Application.DTOs.Dashboard.Performance;
using Application.DTOs.Dashboard.Revenue;
using Application.DTOs.Dashboard.Subscriptions;
using Application.DTOs.Dashboard.TimeSeries;
using Application.DTOs.Dashboard.Trends;

namespace Application.DTOs.Dashboard;

/// <summary>
/// Main dashboard data aggregator with real-time statistics.
/// This follows the Aggregate pattern and Single Responsibility Principle.
/// Each sub-DTO is defined in its own feature-based namespace.
/// </summary>
public class DashboardDto
{
    /// <summary>
    /// Overall system statistics and KPIs
    /// </summary>
    public OverviewStatsDto Overview { get; set; } = new();

    /// <summary>
    /// Company-related statistics
    /// </summary>
    public CompanyStatsDto Companies { get; set; } = new();

    /// <summary>
    /// Subscription-related statistics
    /// </summary>
    public SubscriptionStatsDto Subscriptions { get; set; } = new();

    /// <summary>
    /// Admin-related statistics
    /// </summary>
    public AdminStatsDto Admins { get; set; } = new();

    /// <summary>
    /// Alerts and warnings requiring attention
    /// </summary>
    public AlertsDto Alerts { get; set; } = new();

    /// <summary>
    /// Recent activity summary
    /// </summary>
    public RecentActivityDto RecentActivity { get; set; } = new();

    /// <summary>
    /// Time-series historical data (30 days)
    /// </summary>
    public TimeSeriesDto TimeSeries { get; set; } = new();

    /// <summary>
    /// Revenue tracking and financial analytics
    /// </summary>
    public RevenueDto Revenue { get; set; } = new();

    /// <summary>
    /// Company lifecycle and churn analytics
    /// </summary>
    public LifecycleDto Lifecycle { get; set; } = new();

    /// <summary>
    /// Trend analysis and predictive forecasting
    /// </summary>
    public TrendsDto Trends { get; set; } = new();

    /// <summary>
    /// Admin activity and performance analytics (Phase 5)
    /// </summary>
    public AdminPerformanceDto AdminPerformance { get; set; } = new();

    /// <summary>
    /// Dashboard generation timestamp (UTC)
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

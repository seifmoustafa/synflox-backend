using Application.DTOs.Dashboard;
using Application.DTOs.Dashboard.Activity;
using Application.DTOs.Dashboard.Admins;
using Application.DTOs.Dashboard.Alerts;
using Application.DTOs.Dashboard.Companies;
using Application.DTOs.Dashboard.Subscriptions;

namespace Application.Services;

/// <summary>
/// Service interface for dashboard operations
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets the main dashboard with all statistics and metrics (lightweight overview)
    /// </summary>
    Task<DashboardDto> GetDashboardAsync();

    /// <summary>
    /// Gets detailed company analytics
    /// </summary>
    Task<CompanyStatsDto> GetCompanyAnalyticsAsync();

    /// <summary>
    /// Gets detailed subscription analytics
    /// </summary>
    Task<SubscriptionStatsDto> GetSubscriptionAnalyticsAsync();

    /// <summary>
    /// Gets detailed admin analytics
    /// </summary>
    Task<AdminStatsDto> GetAdminAnalyticsAsync();

    /// <summary>
    /// Gets system alerts and warnings
    /// </summary>
    Task<AlertsDto> GetAlertsAsync();

    /// <summary>
    /// Gets recent activity summary
    /// </summary>
    Task<RecentActivityDto> GetRecentActivityAsync();
}


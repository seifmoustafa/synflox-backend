using Application.DTOs.Dashboard.Overview;
using Application.DTOs.Dashboard.Companies;
using Application.DTOs.Dashboard.Subscriptions;
using Application.DTOs.Dashboard.Revenue;
using Application.DTOs.Dashboard.Activity;
using Application.DTOs.Dashboard.Alerts;

namespace Application.Services_Interfaces;

/// <summary>
/// Dashboard service interface for all dashboard analytics
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get overview dashboard with KPIs and quick stats
    /// </summary>
    Task<OverviewDashboardDto> GetOverviewAsync();
    
    /// <summary>
    /// Get companies dashboard with company analytics
    /// </summary>
    Task<CompaniesDashboardDto> GetCompaniesDashboardAsync();
    
    /// <summary>
    /// Get subscriptions dashboard with subscription analytics
    /// </summary>
    Task<SubscriptionsDashboardDto> GetSubscriptionsDashboardAsync();
    
    /// <summary>
    /// Get revenue dashboard with financial analytics
    /// Requires SuperAdmin access
    /// </summary>
    Task<RevenueDashboardDto> GetRevenueDashboardAsync();
    
    /// <summary>
    /// Get activity dashboard with admin activity analytics
    /// </summary>
    Task<ActivityDashboardDto> GetActivityDashboardAsync();
    
    /// <summary>
    /// Get alerts dashboard with system alerts
    /// </summary>
    Task<AlertsDashboardDto> GetAlertsDashboardAsync();
    
    /// <summary>
    /// Dismiss an alert
    /// </summary>
    Task DismissAlertAsync(Guid alertId, Guid adminId);
    
    /// <summary>
    /// Mark alert as read
    /// </summary>
    Task MarkAlertAsReadAsync(Guid alertId, Guid adminId);
}

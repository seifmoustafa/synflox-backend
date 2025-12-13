using Application.DTOs.Dashboard.Overview;
using Application.DTOs.Dashboard.Companies;
using Application.DTOs.Dashboard.Subscriptions;
using Application.DTOs.Dashboard.Revenue;
using Application.DTOs.Dashboard.Activity;
using Application.DTOs.Dashboard.Alerts;
using Application.DTOs.Dashboard.Shared;
using Domain.Enums;

namespace Application.Services_Interfaces;

/// <summary>
/// Dashboard service interface for all dashboard analytics
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get overview dashboard with KPIs and quick stats
    /// </summary>
    /// <param name="displayCurrency">Currency to display all monetary amounts in</param>
    Task<OverviewDashboardDto> GetOverviewAsync(Currency displayCurrency = Currency.USD);
    
    /// <summary>
    /// Get companies dashboard with company analytics
    /// </summary>
    /// <param name="displayCurrency">Currency to display all monetary amounts in</param>
    Task<CompaniesDashboardDto> GetCompaniesDashboardAsync(Currency displayCurrency = Currency.USD);
    
    /// <summary>
    /// Get subscriptions dashboard with subscription analytics
    /// </summary>
    /// <param name="displayCurrency">Currency to display all monetary amounts in</param>
    Task<SubscriptionsDashboardDto> GetSubscriptionsDashboardAsync(Currency displayCurrency = Currency.USD);
    
    /// <summary>
    /// Get revenue dashboard with financial analytics
    /// Requires SuperAdmin access
    /// </summary>
    /// <param name="displayCurrency">Currency to display all amounts in (real-time conversion)</param>
    Task<RevenueDashboardDto> GetRevenueDashboardAsync(Currency displayCurrency = Currency.USD);
    
    /// <summary>
    /// Get available currencies with current exchange rates
    /// </summary>
    /// <param name="baseCurrency">Base currency for rates</param>
    Task<CurrencyRatesDto> GetExchangeRatesAsync(Currency baseCurrency = Currency.USD);
    
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

using Application.DTOs.Dashboard.Overview;
using Application.DTOs.Dashboard.Companies;
using Application.DTOs.Dashboard.Subscriptions;
using Application.DTOs.Dashboard.Revenue;
using Application.DTOs.Dashboard.Activity;
using Application.DTOs.Dashboard.Alerts;
using Application.DTOs.Dashboard.Shared;
using Application.Services_Interfaces;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Dashboard analytics endpoints
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILocalizationService localizer,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Get overview dashboard with KPIs and quick stats
    /// </summary>
    /// <param name="currency">Currency to display all monetary amounts in (default: USD)</param>
    [HttpGet("overview")]
    [ResponseCache(Duration = 60, VaryByHeader = "Authorization")]
    public async Task<ActionResult<OverviewDashboardDto>> GetOverview(
        [FromQuery] Currency currency = Currency.USD)
    {
        _logger.LogInformation("Getting overview dashboard in {Currency}", currency);
        var result = await _dashboardService.GetOverviewAsync(currency);
        return Ok(result);
    }

    /// <summary>
    /// Get companies dashboard with company analytics
    /// </summary>
    /// <param name="currency">Currency to display all monetary amounts in (default: USD)</param>
    [HttpGet("companies")]
    [ResponseCache(Duration = 60, VaryByHeader = "Authorization")]
    public async Task<ActionResult<CompaniesDashboardDto>> GetCompaniesDashboard(
        [FromQuery] Currency currency = Currency.USD)
    {
        _logger.LogInformation("Getting companies dashboard in {Currency}", currency);
        var result = await _dashboardService.GetCompaniesDashboardAsync(currency);
        return Ok(result);
    }

    /// <summary>
    /// Get subscriptions dashboard with subscription analytics
    /// </summary>
    /// <param name="currency">Currency to display all monetary amounts in (default: USD)</param>
    [HttpGet("subscriptions")]
    [ResponseCache(Duration = 60, VaryByHeader = "Authorization")]
    public async Task<ActionResult<SubscriptionsDashboardDto>> GetSubscriptionsDashboard(
        [FromQuery] Currency currency = Currency.USD)
    {
        _logger.LogInformation("Getting subscriptions dashboard in {Currency}", currency);
        var result = await _dashboardService.GetSubscriptionsDashboardAsync(currency);
        return Ok(result);
    }

    /// <summary>
    /// Get revenue dashboard with financial analytics
    /// SuperAdmin only
    /// </summary>
    /// <param name="currency">Currency to display all amounts in (default: USD). Real-time conversion.</param>
    [HttpGet("revenue")]
    [Authorize(Policy = "SuperAdminOnly")]
    [ResponseCache(Duration = 60, VaryByHeader = "Authorization")]
    public async Task<ActionResult<RevenueDashboardDto>> GetRevenueDashboard(
        [FromQuery] Currency currency = Currency.USD)
    {
        _logger.LogInformation("Getting revenue dashboard in {Currency}", currency);
        var result = await _dashboardService.GetRevenueDashboardAsync(currency);
        return Ok(result);
    }
    
    /// <summary>
    /// Get current exchange rates for all supported currencies
    /// </summary>
    /// <param name="baseCurrency">Base currency for rates (default: USD)</param>
    [HttpGet("exchange-rates")]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour (rates update daily)
    public async Task<ActionResult<CurrencyRatesDto>> GetExchangeRates(
        [FromQuery] Currency baseCurrency = Currency.USD)
    {
        _logger.LogInformation("Getting exchange rates for base {Currency}", baseCurrency);
        var result = await _dashboardService.GetExchangeRatesAsync(baseCurrency);
        return Ok(result);
    }

    /// <summary>
    /// Get activity dashboard with admin activity analytics
    /// </summary>
    [HttpGet("activity")]
    [ResponseCache(Duration = 60, VaryByHeader = "Authorization")]
    public async Task<ActionResult<ActivityDashboardDto>> GetActivityDashboard()
    {
        _logger.LogInformation("Getting activity dashboard");
        var result = await _dashboardService.GetActivityDashboardAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get alerts dashboard with system alerts
    /// </summary>
    [HttpGet("alerts")]
    [ResponseCache(Duration = 30, VaryByHeader = "Authorization")]
    public async Task<ActionResult<AlertsDashboardDto>> GetAlertsDashboard()
    {
        _logger.LogInformation("Getting alerts dashboard");
        var result = await _dashboardService.GetAlertsDashboardAsync();
        return Ok(result);
    }

    /// <summary>
    /// Dismiss an alert
    /// </summary>
    [HttpPost("alerts/{alertId}/dismiss")]
    public async Task<IActionResult> DismissAlert(Guid alertId)
    {
        // Get current admin ID from claims
        var adminIdClaim = User.FindFirst("AdminId")?.Value;
        if (string.IsNullOrEmpty(adminIdClaim) || !Guid.TryParse(adminIdClaim, out var adminId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Dismissing alert {AlertId} by admin {AdminId}", alertId, adminId);
        await _dashboardService.DismissAlertAsync(alertId, adminId);
        return Ok(new { message = _localizer["Dashboard.AlertDismissed"] });
    }

    /// <summary>
    /// Mark alert as read
    /// </summary>
    [HttpPost("alerts/{alertId}/read")]
    public async Task<IActionResult> MarkAlertAsRead(Guid alertId)
    {
        var adminIdClaim = User.FindFirst("AdminId")?.Value;
        if (string.IsNullOrEmpty(adminIdClaim) || !Guid.TryParse(adminIdClaim, out var adminId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Marking alert {AlertId} as read by admin {AdminId}", alertId, adminId);
        await _dashboardService.MarkAlertAsReadAsync(alertId, adminId);
        return Ok(new { message = _localizer["Dashboard.AlertRead"] });
    }
}

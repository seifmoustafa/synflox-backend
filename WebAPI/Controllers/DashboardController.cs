using Application.DTOs.Dashboard.Overview;
using Application.DTOs.Dashboard.Companies;
using Application.DTOs.Dashboard.Subscriptions;
using Application.DTOs.Dashboard.Revenue;
using Application.DTOs.Dashboard.Activity;
using Application.DTOs.Dashboard.Alerts;
using Application.Services_Interfaces;
using Application.Services;
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
    [HttpGet("overview")]
    public async Task<ActionResult<OverviewDashboardDto>> GetOverview()
    {
        _logger.LogInformation("Getting overview dashboard");
        var result = await _dashboardService.GetOverviewAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get companies dashboard with company analytics
    /// </summary>
    [HttpGet("companies")]
    public async Task<ActionResult<CompaniesDashboardDto>> GetCompaniesDashboard()
    {
        _logger.LogInformation("Getting companies dashboard");
        var result = await _dashboardService.GetCompaniesDashboardAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get subscriptions dashboard with subscription analytics
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<ActionResult<SubscriptionsDashboardDto>> GetSubscriptionsDashboard()
    {
        _logger.LogInformation("Getting subscriptions dashboard");
        var result = await _dashboardService.GetSubscriptionsDashboardAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get revenue dashboard with financial analytics
    /// SuperAdmin only
    /// </summary>
    [HttpGet("revenue")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<RevenueDashboardDto>> GetRevenueDashboard()
    {
        _logger.LogInformation("Getting revenue dashboard");
        var result = await _dashboardService.GetRevenueDashboardAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get activity dashboard with admin activity analytics
    /// </summary>
    [HttpGet("activity")]
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

using Application.DTOs.Dashboard;
using Application.DTOs.Dashboard.Activity;
using Application.DTOs.Dashboard.Admins;
using Application.DTOs.Dashboard.Alerts;
using Application.DTOs.Dashboard.Companies;
using Application.DTOs.Dashboard.Subscriptions;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Dashboard Controller - Clean and Professional
/// Provides system statistics and metrics
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILocalizationService _localizer;

    public DashboardController(IDashboardService dashboardService, ILocalizationService localizer)
    {
        _dashboardService = dashboardService;
        _localizer = localizer;
    }

    /// <summary>
    /// Gets the main dashboard with all statistics and metrics (lightweight overview)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var result = await _dashboardService.GetDashboardAsync();
            return Ok(new ApiResponse<DashboardDto>(200, _localizer["Dashboard.Retrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets detailed company analytics
    /// </summary>
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanyAnalytics()
    {
        try
        {
            var result = await _dashboardService.GetCompanyAnalyticsAsync();
            return Ok(new ApiResponse<CompanyStatsDto>(200, _localizer["Dashboard.CompaniesRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets detailed subscription analytics
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptionAnalytics()
    {
        try
        {
            var result = await _dashboardService.GetSubscriptionAnalyticsAsync();
            return Ok(new ApiResponse<SubscriptionStatsDto>(200, _localizer["Dashboard.SubscriptionsRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets detailed admin analytics
    /// </summary>
    [HttpGet("admins")]
    public async Task<IActionResult> GetAdminAnalytics()
    {
        try
        {
            var result = await _dashboardService.GetAdminAnalyticsAsync();
            return Ok(new ApiResponse<AdminStatsDto>(200, _localizer["Dashboard.AdminsRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets system alerts and warnings
    /// </summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts()
    {
        try
        {
            var result = await _dashboardService.GetAlertsAsync();
            return Ok(new ApiResponse<AlertsDto>(200, _localizer["Dashboard.AlertsRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets recent activity summary
    /// </summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetRecentActivity()
    {
        try
        {
            var result = await _dashboardService.GetRecentActivityAsync();
            return Ok(new ApiResponse<RecentActivityDto>(200, _localizer["Dashboard.ActivityRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }
}


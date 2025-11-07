using Application.DTOs.Dashboard;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for system dashboard and endpoint discovery
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
    /// Gets all API endpoints in the system
    /// </summary>
    /// <returns>List of all endpoints grouped by controller</returns>
    [HttpGet("endpoints")]
    public async Task<IActionResult> GetAllEndpoints()
    {
        try
        {
            var result = await _dashboardService.GetAllEndpointsAsync();
            return Ok(new ApiResponse<DashboardResponseDto>(200, _localizer["Dashboard.EndpointsRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets system-wide statistics for the dashboard
    /// </summary>
    /// <returns>System statistics including counts and license status breakdown</returns>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetSystemStatistics()
    {
        try
        {
            var result = await _dashboardService.GetSystemStatisticsAsync();
            return Ok(new ApiResponse<SystemStatisticsDto>(200, _localizer["Dashboard.StatisticsRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets complete dashboard overview (statistics + endpoints)
    /// </summary>
    /// <returns>Complete dashboard data</returns>
    [HttpGet("overview")]
    public async Task<IActionResult> GetDashboardOverview()
    {
        try
        {
            var result = await _dashboardService.GetDashboardOverviewAsync();
            return Ok(new ApiResponse<DashboardOverviewDto>(200, _localizer["Dashboard.OverviewRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }
}


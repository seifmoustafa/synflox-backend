using Application.DTOs.Dashboard;

namespace Application.Services;

/// <summary>
/// Service interface for dashboard operations
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets all API endpoints in the system
    /// </summary>
    /// <returns>Dashboard response containing all endpoints</returns>
    Task<DashboardResponseDto> GetAllEndpointsAsync();

    /// <summary>
    /// Gets system-wide statistics for the dashboard
    /// </summary>
    /// <returns>System statistics including counts and license status breakdown</returns>
    Task<SystemStatisticsDto> GetSystemStatisticsAsync();

    /// <summary>
    /// Gets complete dashboard overview (statistics + endpoints)
    /// </summary>
    /// <returns>Complete dashboard data</returns>
    Task<DashboardOverviewDto> GetDashboardOverviewAsync();
}


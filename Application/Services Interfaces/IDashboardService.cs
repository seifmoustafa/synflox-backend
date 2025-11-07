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
}


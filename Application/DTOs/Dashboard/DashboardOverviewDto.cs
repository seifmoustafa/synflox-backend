namespace Application.DTOs.Dashboard;

/// <summary>
/// Complete dashboard overview containing statistics and endpoints
/// </summary>
public class DashboardOverviewDto
{
    /// <summary>
    /// System statistics
    /// </summary>
    public SystemStatisticsDto Statistics { get; set; } = new();

    /// <summary>
    /// All API endpoints
    /// </summary>
    public DashboardResponseDto Endpoints { get; set; } = new();
}


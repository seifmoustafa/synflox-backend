namespace Application.DTOs.Dashboard;

/// <summary>
/// Response DTO for the dashboard endpoint listing all API endpoints
/// </summary>
public class DashboardResponseDto
{
    /// <summary>
    /// Total number of endpoints
    /// </summary>
    public int TotalEndpoints { get; set; }

    /// <summary>
    /// List of all endpoints grouped by controller
    /// </summary>
    public Dictionary<string, List<EndpointInfoDto>> EndpointsByController { get; set; } = new();

    /// <summary>
    /// All endpoints in a flat list
    /// </summary>
    public List<EndpointInfoDto> AllEndpoints { get; set; } = new();
}


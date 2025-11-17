namespace Application.DTOs.Dashboard.Overview;

/// <summary>
/// Overview statistics and KPIs
/// </summary>
public class OverviewStatsDto
{
    public int TotalCompanies { get; set; }
    public int ActiveCompanies { get; set; }
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveAdmins { get; set; }
}

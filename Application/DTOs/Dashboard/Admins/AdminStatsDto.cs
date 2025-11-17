namespace Application.DTOs.Dashboard.Admins;

/// <summary>
/// Admin statistics
/// </summary>
public class AdminStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int CreatedToday { get; set; }
    public int CreatedThisWeek { get; set; }
    public int CreatedThisMonth { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
}

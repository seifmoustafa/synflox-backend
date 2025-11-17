namespace Application.DTOs.Dashboard.Companies;

/// <summary>
/// Company statistics categorized by their subscription/license status
/// </summary>
public class CompanyStatsDto
{
    public int Total { get; set; }
    public int ActiveLicense { get; set; }  // Companies with active subscription
    public int SuspendedLicense { get; set; }  // Companies with suspended subscription
    public int ExpiredLicense { get; set; }  // Companies with expired/no subscription
    public int CreatedToday { get; set; }
    public int CreatedThisWeek { get; set; }
    public int CreatedThisMonth { get; set; }
}

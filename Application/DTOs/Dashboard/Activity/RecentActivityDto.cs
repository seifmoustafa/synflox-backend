namespace Application.DTOs.Dashboard.Activity;

/// <summary>
/// Recent activity summary
/// </summary>
public class RecentActivityDto
{
    public int CompaniesLast24Hours { get; set; }
    public int SubscriptionsLast24Hours { get; set; }
    public int AdminsLast24Hours { get; set; }
}

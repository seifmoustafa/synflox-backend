namespace Application.DTOs.Dashboard.Subscriptions;

/// <summary>
/// Subscription statistics
/// </summary>
public class SubscriptionStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Trial { get; set; }
    public int Expired { get; set; }
    public int Suspended { get; set; }
    public int ExpiringWithin7Days { get; set; }
    public int ExpiringWithin30Days { get; set; }
    public int CreatedToday { get; set; }
    public int CreatedThisWeek { get; set; }
    public int CreatedThisMonth { get; set; }
    public Dictionary<string, int> ByPlan { get; set; } = new();
}

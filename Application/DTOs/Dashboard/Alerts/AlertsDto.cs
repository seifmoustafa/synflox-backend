namespace Application.DTOs.Dashboard.Alerts;

/// <summary>
/// System alerts and warnings
/// </summary>
public class AlertsDto
{
    public int SubscriptionsExpiringToday { get; set; }
    public int SubscriptionsExpiringThisWeek { get; set; }
    public int CompaniesWithSuspendedLicense { get; set; }  // Companies that have suspended subscription
    public int CompaniesWithExpiredLicense { get; set; }  // Companies with expired or no subscription
    public int InactiveAdmins { get; set; }
    public List<string> Messages { get; set; } = new();
}

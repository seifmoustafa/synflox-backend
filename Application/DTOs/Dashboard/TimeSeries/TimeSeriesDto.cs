namespace Application.DTOs.Dashboard.TimeSeries;

/// <summary>
/// Time-series data for 30-day historical tracking
/// </summary>
public class TimeSeriesDto
{
    public List<DailyMetricDto> Last30Days { get; set; } = new();
}

/// <summary>
/// Daily metrics for historical tracking
/// </summary>
public class DailyMetricDto
{
    public DateTime Date { get; set; }
    public int CompaniesCreated { get; set; }
    public int SubscriptionsCreated { get; set; }
    public int AdminsCreated { get; set; }
    public int ActiveCompanies { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ActiveAdmins { get; set; }
}

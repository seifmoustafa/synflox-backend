namespace Application.DTOs.Dashboard.Performance;

/// <summary>
/// Admin activity and performance analytics
/// </summary>
public class AdminPerformanceDto
{
    /// <summary>
    /// Performance leaderboard (top admins)
    /// </summary>
    public List<AdminPerformanceMetricDto> Leaderboard { get; set; } = new();

    /// <summary>
    /// Activity heatmap for last 30 days
    /// </summary>
    public List<ActivityHeatmapDto> ActivityHeatmap { get; set; } = new();

    /// <summary>
    /// Performance comparison by admin type
    /// </summary>
    public List<AdminTypePerformanceDto> TypePerformance { get; set; } = new();

    /// <summary>
    /// System-wide activity statistics
    /// </summary>
    public SystemActivityStatsDto SystemActivity { get; set; } = new();

    /// <summary>
    /// Peak activity hours
    /// </summary>
    public List<PeakActivityHourDto> PeakHours { get; set; } = new();
}

/// <summary>
/// Individual admin performance metrics
/// </summary>
public class AdminPerformanceMetricDto
{
    /// <summary>
    /// Admin encrypted ID
    /// </summary>
    public Guid AdminId { get; set; }

    /// <summary>
    /// Admin username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Admin full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Admin type/role
    /// </summary>
    public string AdminType { get; set; } = string.Empty;

    /// <summary>
    /// Total actions performed
    /// </summary>
    public int TotalActions { get; set; }

    /// <summary>
    /// Number of login sessions
    /// </summary>
    public int LoginCount { get; set; }

    /// <summary>
    /// Companies managed by this admin
    /// </summary>
    public int CompaniesManaged { get; set; }

    /// <summary>
    /// Subscriptions managed by this admin
    /// </summary>
    public int SubscriptionsManaged { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AvgResponseTime { get; set; }

    /// <summary>
    /// Performance score (0-100)
    /// </summary>
    public int PerformanceScore { get; set; }

    /// <summary>
    /// Activity level classification
    /// </summary>
    public string ActivityLevel { get; set; } = string.Empty; // "High", "Medium", "Low"

    /// <summary>
    /// Last active timestamp
    /// </summary>
    public DateTime LastActiveDate { get; set; }

    /// <summary>
    /// Number of days active in last 30 days
    /// </summary>
    public int DaysActive { get; set; }
}

/// <summary>
/// Activity heatmap data for a specific day
/// </summary>
public class ActivityHeatmapDto
{
    /// <summary>
    /// Date of activity
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Day of week (0 = Sunday, 6 = Saturday)
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// Hourly activity breakdown (24 hours)
    /// </summary>
    public List<int> HourlyActivity { get; set; } = new();

    /// <summary>
    /// Total activity for the day
    /// </summary>
    public int TotalActivity { get; set; }

    /// <summary>
    /// Peak hour of activity (0-23)
    /// </summary>
    public int PeakHour { get; set; }
}

/// <summary>
/// Performance metrics by admin type
/// </summary>
public class AdminTypePerformanceDto
{
    /// <summary>
    /// Admin type name
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Number of admins of this type
    /// </summary>
    public int AdminCount { get; set; }

    /// <summary>
    /// Average performance score for this type
    /// </summary>
    public double AvgPerformanceScore { get; set; }

    /// <summary>
    /// Total actions by this admin type
    /// </summary>
    public int TotalActions { get; set; }

    /// <summary>
    /// Average actions per admin of this type
    /// </summary>
    public double AvgActionsPerAdmin { get; set; }

    /// <summary>
    /// Percentage of active admins of this type
    /// </summary>
    public double ActivePercentage { get; set; }
}

/// <summary>
/// Overall system activity statistics
/// </summary>
public class SystemActivityStatsDto
{
    /// <summary>
    /// Total actions in period
    /// </summary>
    public int TotalActions { get; set; }

    /// <summary>
    /// Total login sessions in period
    /// </summary>
    public int TotalLogins { get; set; }

    /// <summary>
    /// Average actions per day
    /// </summary>
    public double AvgActionsPerDay { get; set; }

    /// <summary>
    /// Number of active admins
    /// </summary>
    public int ActiveAdmins { get; set; }

    /// <summary>
    /// Peak activity date
    /// </summary>
    public DateTime PeakActivityDate { get; set; }

    /// <summary>
    /// Peak activity count
    /// </summary>
    public int PeakActivityCount { get; set; }

    /// <summary>
    /// Average admins online simultaneously
    /// </summary>
    public double AvgAdminsOnline { get; set; }
}

/// <summary>
/// Peak activity hours
/// </summary>
public class PeakActivityHourDto
{
    /// <summary>
    /// Hour of day (0-23)
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Total activity count for this hour
    /// </summary>
    public int ActivityCount { get; set; }

    /// <summary>
    /// Percentage of total activity
    /// </summary>
    public double Percentage { get; set; }
}

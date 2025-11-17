namespace Application.DTOs.Dashboard.Trends;

/// <summary>
/// Trend analysis and predictive forecasting for overview metrics
/// </summary>
public class TrendsDto
{
    /// <summary>
    /// Company growth trends and forecasts
    /// </summary>
    public EntityTrendDto Companies { get; set; } = new();

    /// <summary>
    /// Subscription growth trends and forecasts
    /// </summary>
    public EntityTrendDto Subscriptions { get; set; } = new();

    /// <summary>
    /// Admin growth trends and forecasts
    /// </summary>
    public EntityTrendDto Admins { get; set; } = new();

    /// <summary>
    /// Overall system health trend
    /// </summary>
    public TrendDirection SystemHealthTrend { get; set; }

    /// <summary>
    /// Growth velocity indicator
    /// </summary>
    public string GrowthVelocity { get; set; } = string.Empty; // "Accelerating", "Steady", "Decelerating"
}

/// <summary>
/// Trend data for a specific entity type
/// </summary>
public class EntityTrendDto
{
    /// <summary>
    /// Current total count
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    /// Total count from previous period (last month)
    /// </summary>
    public int Previous { get; set; }

    /// <summary>
    /// Percentage change from previous period
    /// </summary>
    public decimal ChangePercent { get; set; }

    /// <summary>
    /// Trend direction
    /// </summary>
    public TrendDirection Direction { get; set; }

    /// <summary>
    /// Week-over-week change
    /// </summary>
    public int WeekOverWeekChange { get; set; }

    /// <summary>
    /// Month-over-month change
    /// </summary>
    public int MonthOverMonthChange { get; set; }

    /// <summary>
    /// Forecasted total for next 30 days
    /// </summary>
    public int Forecast30Days { get; set; }

    /// <summary>
    /// Daily growth rate
    /// </summary>
    public decimal DailyGrowthRate { get; set; }
}

/// <summary>
/// Trend direction enumeration
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Significant upward trend (>5% increase)
    /// </summary>
    StrongUp = 0,

    /// <summary>
    /// Moderate upward trend (1-5% increase)
    /// </summary>
    Up = 1,

    /// <summary>
    /// Stable, minimal change (-1% to +1%)
    /// </summary>
    Stable = 2,

    /// <summary>
    /// Moderate downward trend (1-5% decrease)
    /// </summary>
    Down = 3,

    /// <summary>
    /// Significant downward trend (>5% decrease)
    /// </summary>
    StrongDown = 4
}

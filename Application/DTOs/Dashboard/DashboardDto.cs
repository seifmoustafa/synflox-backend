namespace Application.DTOs.Dashboard;

/// <summary>
/// Main dashboard data with real-time statistics
/// </summary>
public class DashboardDto
{
    /// <summary>
    /// Overall system statistics
    /// </summary>
    public OverviewStatsDto Overview { get; set; } = new();

    /// <summary>
    /// Company-related statistics
    /// </summary>
    public CompanyStatsDto Companies { get; set; } = new();

    /// <summary>
    /// Subscription-related statistics
    /// </summary>
    public SubscriptionStatsDto Subscriptions { get; set; } = new();

    /// <summary>
    /// Admin-related statistics
    /// </summary>
    public AdminStatsDto Admins { get; set; } = new();

    /// <summary>
    /// Alerts and warnings requiring attention
    /// </summary>
    public AlertsDto Alerts { get; set; } = new();

    /// <summary>
    /// Recent activity summary
    /// </summary>
    public RecentActivityDto RecentActivity { get; set; } = new();

    /// <summary>
    /// Time-series historical data (30 days)
    /// </summary>
    public TimeSeriesDto TimeSeries { get; set; } = new();

    /// <summary>
    /// Revenue tracking and financial analytics
    /// </summary>
    public RevenueDto Revenue { get; set; } = new();

    /// <summary>
    /// Dashboard generation timestamp
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

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

/// <summary>
/// Recent activity summary
/// </summary>
public class RecentActivityDto
{
    public int CompaniesLast24Hours { get; set; }
    public int SubscriptionsLast24Hours { get; set; }
    public int AdminsLast24Hours { get; set; }
}

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
    public int CompaniesActive { get; set; }
    public int SubscriptionsActive { get; set; }
    public int AdminsActive { get; set; }
}

/// <summary>
/// Revenue tracking and financial analytics
/// </summary>
public class RevenueDto
{
    /// <summary>
    /// Monthly Recurring Revenue (normalized to USD)
    /// </summary>
    public decimal MRR { get; set; }

    /// <summary>
    /// Annual Recurring Revenue (MRR * 12)
    /// </summary>
    public decimal ARR { get; set; }

    /// <summary>
    /// Total revenue from all active subscriptions
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Average Revenue Per Customer (ARPC)
    /// </summary>
    public decimal ARPC { get; set; }

    /// <summary>
    /// Revenue breakdown by subscription plan
    /// Key: Plan Name, Value: Revenue Amount
    /// </summary>
    public Dictionary<string, decimal> RevenueByPlan { get; set; } = new();

    /// <summary>
    /// Revenue breakdown by currency
    /// Key: Currency Code (USD, EUR, etc.), Value: Revenue Amount
    /// </summary>
    public Dictionary<string, decimal> RevenueByCurrency { get; set; } = new();

    /// <summary>
    /// Revenue growth over time (last 12 months)
    /// </summary>
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();

    /// <summary>
    /// Percentage growth from previous month
    /// </summary>
    public decimal MonthOverMonthGrowth { get; set; }

    /// <summary>
    /// Number of paying customers (active non-trial subscriptions)
    /// </summary>
    public int PayingCustomers { get; set; }

    /// <summary>
    /// Total number of trial subscriptions
    /// </summary>
    public int TrialSubscriptions { get; set; }
}

/// <summary>
/// Monthly revenue data point
/// </summary>
public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;  // Format: "2024-01" or "Jan 2024"
    public decimal Revenue { get; set; }
    public int SubscriptionCount { get; set; }
    public decimal AverageRevenuePerSubscription { get; set; }
}

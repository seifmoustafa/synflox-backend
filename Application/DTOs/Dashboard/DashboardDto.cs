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
    /// Company lifecycle and churn analytics
    /// </summary>
    public LifecycleDto Lifecycle { get; set; } = new();

    /// <summary>
    /// Trend analysis and predictive forecasting
    /// </summary>
    public TrendsDto Trends { get; set; } = new();

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

/// <summary>
/// Company lifecycle and churn analytics
/// </summary>
public class LifecycleDto
{
    /// <summary>
    /// Companies in each lifecycle stage
    /// </summary>
    public LifecycleStageDto Stages { get; set; } = new();

    /// <summary>
    /// Churn analysis and risk metrics
    /// </summary>
    public ChurnDto Churn { get; set; } = new();

    /// <summary>
    /// Customer health distribution (0-100 score)
    /// </summary>
    public HealthDistributionDto HealthDistribution { get; set; } = new();

    /// <summary>
    /// Lifecycle transition metrics
    /// </summary>
    public List<LifecycleTransitionDto> Transitions { get; set; } = new();
}

/// <summary>
/// Company count by lifecycle stage
/// </summary>
public class LifecycleStageDto
{
    /// <summary>
    /// New companies (created within last 30 days)
    /// </summary>
    public int New { get; set; }

    /// <summary>
    /// Active companies with healthy subscriptions
    /// </summary>
    public int Active { get; set; }

    /// <summary>
    /// At-risk companies (expiring soon, suspended, or inactive)
    /// </summary>
    public int AtRisk { get; set; }

    /// <summary>
    /// Churned companies (expired subscriptions, no longer active)
    /// </summary>
    public int Churned { get; set; }

    /// <summary>
    /// Returning companies (previously churned, now active again)
    /// </summary>
    public int Returning { get; set; }
}

/// <summary>
/// Churn analysis metrics
/// </summary>
public class ChurnDto
{
    /// <summary>
    /// Overall churn rate percentage
    /// </summary>
    public decimal ChurnRate { get; set; }

    /// <summary>
    /// Number of companies churned this month
    /// </summary>
    public int ChurnedThisMonth { get; set; }

    /// <summary>
    /// Number of companies at high risk of churning
    /// </summary>
    public int HighRiskCount { get; set; }

    /// <summary>
    /// Retention rate percentage
    /// </summary>
    public decimal RetentionRate { get; set; }

    /// <summary>
    /// Average customer lifetime in days
    /// </summary>
    public double AverageLifetimeDays { get; set; }

    /// <summary>
    /// Companies by risk score ranges
    /// </summary>
    public RiskDistributionDto RiskDistribution { get; set; } = new();
}

/// <summary>
/// Risk score distribution
/// </summary>
public class RiskDistributionDto
{
    public int Low { get; set; }        // Score 0-33 (healthy)
    public int Medium { get; set; }     // Score 34-66 (monitor)
    public int High { get; set; }       // Score 67-100 (critical)
}

/// <summary>
/// Health score distribution
/// </summary>
public class HealthDistributionDto
{
    public int Excellent { get; set; }  // Score 80-100
    public int Good { get; set; }       // Score 60-79
    public int Fair { get; set; }       // Score 40-59
    public int Poor { get; set; }       // Score 0-39
}

/// <summary>
/// Lifecycle stage transition data
/// </summary>
public class LifecycleTransitionDto
{
    public string FromStage { get; set; } = string.Empty;
    public string ToStage { get; set; } = string.Empty;
    public int Count { get; set; }
}

// ===========================
// PHASE 4: TRENDS & FORECASTING
// ===========================

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
    /// Growth velocity (acceleration/deceleration)
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
    /// Previous period total (for comparison)
    /// </summary>
    public int Previous { get; set; }

    /// <summary>
    /// Percentage change from previous period
    /// </summary>
    public decimal ChangePercent { get; set; }

    /// <summary>
    /// Trend direction indicator
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
    /// Projected value for next 30 days (simple linear forecast)
    /// </summary>
    public int Forecast30Days { get; set; }

    /// <summary>
    /// Growth rate per day (average)
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
    /// Stable/No significant change (-1% to +1%)
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

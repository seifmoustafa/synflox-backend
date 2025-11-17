namespace Application.DTOs.Dashboard.Revenue;

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
    /// Annual Recurring Revenue (normalized to USD)
    /// </summary>
    public decimal ARR { get; set; }

    /// <summary>
    /// Total revenue from active subscriptions (sum of all subscription amounts in their respective currencies, normalized to USD)
    /// </summary>
    public decimal TotalActiveRevenue { get; set; }

    /// <summary>
    /// Average Revenue Per Company (normalized to USD)
    /// </summary>
    public decimal ARPC { get; set; }

    /// <summary>
    /// Revenue growth percentage vs last month
    /// </summary>
    public decimal GrowthPercentage { get; set; }

    /// <summary>
    /// Monthly revenue breakdown (last 12 months)
    /// </summary>
    public List<MonthlyRevenueDto> MonthlyBreakdown { get; set; } = new();

    /// <summary>
    /// Revenue distribution by currency
    /// </summary>
    public Dictionary<string, decimal> ByCurrency { get; set; } = new();

    /// <summary>
    /// Total revenue (alias for TotalActiveRevenue)
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Revenue distribution by plan
    /// </summary>
    public Dictionary<string, decimal> RevenueByPlan { get; set; } = new();

    /// <summary>
    /// Revenue distribution by currency (alias for ByCurrency)
    /// </summary>
    public Dictionary<string, decimal> RevenueByCurrency { get; set; } = new();

    /// <summary>
    /// Monthly revenue data (alias for MonthlyBreakdown)
    /// </summary>
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();

    /// <summary>
    /// Month-over-month growth percentage (alias for GrowthPercentage)
    /// </summary>
    public decimal MonthOverMonthGrowth { get; set; }

    /// <summary>
    /// Number of paying customers
    /// </summary>
    public int PayingCustomers { get; set; }

    /// <summary>
    /// Number of trial subscriptions
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
    public string CurrencyCode { get; set; } = "USD";
    public int SubscriptionCount { get; set; }
    public decimal AverageRevenuePerSubscription { get; set; }
}

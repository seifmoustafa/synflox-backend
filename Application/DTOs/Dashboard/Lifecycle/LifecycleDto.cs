namespace Application.DTOs.Dashboard.Lifecycle;

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
    /// Churn analysis
    /// </summary>
    public ChurnDto Churn { get; set; } = new();

    /// <summary>
    /// Risk score distribution
    /// </summary>
    public RiskDistributionDto RiskDistribution { get; set; } = new();

    /// <summary>
    /// Health score distribution
    /// </summary>
    public HealthDistributionDto HealthDistribution { get; set; } = new();

    /// <summary>
    /// Lifecycle transitions in the last 30 days
    /// </summary>
    public List<LifecycleTransitionDto> RecentTransitions { get; set; } = new();

    /// <summary>
    /// All lifecycle transitions
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
    /// Companies at risk of churning
    /// </summary>
    public int AtRisk { get; set; }

    /// <summary>
    /// Churned companies (inactive or canceled)
    /// </summary>
    public int Churned { get; set; }

    /// <summary>
    /// Returning companies (reactivated after churn)
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
    /// Number of companies churned last month
    /// </summary>
    public int ChurnedLastMonth { get; set; }

    /// <summary>
    /// Revenue lost due to churn this month
    /// </summary>
    public decimal RevenueLost { get; set; }

    /// <summary>
    /// Number of high-risk companies
    /// </summary>
    public int HighRiskCount { get; set; }

    /// <summary>
    /// Customer retention rate percentage
    /// </summary>
    public decimal RetentionRate { get; set; }

    /// <summary>
    /// Average customer lifetime in days
    /// </summary>
    public double AverageLifetimeDays { get; set; }

    /// <summary>
    /// Risk score distribution
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
    public int High { get; set; }       // Score 67-100 (at risk)
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

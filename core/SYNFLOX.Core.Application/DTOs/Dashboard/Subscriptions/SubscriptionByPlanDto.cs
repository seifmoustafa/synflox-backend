namespace Application.DTOs.Dashboard.Subscriptions;

/// <summary>
/// Subscription count and revenue per plan
/// </summary>
public record SubscriptionByPlanDto(
    Guid PlanId,
    string PlanName,
    string PlanTier,            // "Basic", "Pro", "Enterprise", etc.
    int ActiveCount,
    int TrialCount,
    int TotalCount,
    decimal MonthlyRevenue,
    decimal TotalRevenue,
    decimal Percentage,         // % of total subscriptions
    string Color
);

/// <summary>
/// Summary of subscriptions by plan
/// </summary>
public record SubscriptionsByPlanSummaryDto(
    List<SubscriptionByPlanDto> Plans,
    string TopPlanName,
    int TopPlanCount,
    decimal TotalMonthlyRevenue
);

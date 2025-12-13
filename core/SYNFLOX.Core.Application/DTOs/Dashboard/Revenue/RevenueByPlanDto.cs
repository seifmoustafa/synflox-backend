namespace Application.DTOs.Dashboard.Revenue;

/// <summary>
/// Revenue breakdown by plan
/// </summary>
public record RevenueByPlanItemDto(
    Guid PlanId,
    string PlanName,
    string PlanTier,
    decimal MonthlyRevenue,
    decimal AnnualRevenue,
    int SubscriptionCount,
    decimal Percentage,
    string Color
);

/// <summary>
/// Revenue by plan summary
/// </summary>
public record RevenueByPlanDto(
    List<RevenueByPlanItemDto> Plans,
    string TopRevenuePlanName,
    decimal TopRevenuePlanValue,
    decimal TotalRevenue
);

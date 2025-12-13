namespace Application.DTOs.Dashboard.Subscriptions;

/// <summary>
/// Subscription expiry item
/// </summary>
public record ExpiringSubscriptionDto(
    Guid SubscriptionId,
    Guid CompanyId,
    string CompanyName,
    string PlanName,
    DateTime ExpiryDate,
    int DaysRemaining,
    decimal MonthlyValue,
    string Priority             // "critical", "high", "medium"
);

/// <summary>
/// Expiry timeline groupings
/// </summary>
public record ExpiryTimelineDto(
    // Today
    List<ExpiringSubscriptionDto> ExpiringToday,
    int ExpiringTodayCount,
    decimal ExpiringTodayValue,
    
    // This Week (next 7 days)
    List<ExpiringSubscriptionDto> ExpiringThisWeek,
    int ExpiringThisWeekCount,
    decimal ExpiringThisWeekValue,
    
    // This Month (next 30 days)
    List<ExpiringSubscriptionDto> ExpiringThisMonth,
    int ExpiringThisMonthCount,
    decimal ExpiringThisMonthValue,
    
    // Next 3 Months (next 90 days)
    List<ExpiringSubscriptionDto> ExpiringNext3Months,
    int ExpiringNext3MonthsCount,
    decimal ExpiringNext3MonthsValue
);

namespace Application.DTOs.Dashboard.Alerts;

/// <summary>
/// Alert counts by priority
/// </summary>
public record AlertCountsDto(
    int Critical,
    int High,
    int Medium,
    int Low,
    int Total,
    int Unread,
    int Dismissed
);

/// <summary>
/// Alert counts by category
/// </summary>
public record AlertsByCategoryDto(
    int SubscriptionExpiry,
    int SubscriptionStatus,
    int CompanyStatus,
    int AdminActivity,
    int SystemHealth,
    int Revenue
);

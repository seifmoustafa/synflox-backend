namespace Application.DTOs.Dashboard.Companies;

/// <summary>
/// Top company by subscription value
/// </summary>
public record TopCompanyDto(
    Guid Id,
    string Name,
    string? LogoUrl,
    int ActiveSubscriptions,
    decimal TotalValue,         // Sum of subscription prices
    string TopPlanName,         // Highest tier plan
    DateTime? LatestSubscriptionDate,
    string Status               // Active, AtRisk, etc.
);

/// <summary>
/// Top companies list with summary
/// </summary>
public record TopCompaniesDto(
    List<TopCompanyDto> Companies,
    decimal TotalRevenue,
    int TotalSubscriptions
);

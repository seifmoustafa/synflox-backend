namespace Application.DTOs.Dashboard.Revenue;

/// <summary>
/// Revenue projection data
/// </summary>
public record RevenueProjectionDto(
    // Expected from renewals
    decimal ExpectedRenewalRevenue,
    int ExpectedRenewals,
    
    // At risk from expirations
    decimal AtRiskRevenue,
    int AtRiskSubscriptions,
    
    // Projected next month
    decimal ProjectedNextMonthMRR,
    decimal ProjectedNextMonthChange,
    
    // Projected next quarter
    decimal ProjectedNextQuarterRevenue,
    
    // Based on current churn rate
    decimal ProjectedChurnImpact,
    
    // Best/Worst case scenarios
    decimal BestCaseProjection,
    decimal WorstCaseProjection
);

using Application.DTOs.Dashboard.Shared;

namespace Application.DTOs.Dashboard.Companies;

/// <summary>
/// Companies dashboard response with all company analytics
/// </summary>
public record CompaniesDashboardDto(
    // Summary Stats
    int TotalCompanies,
    int NewThisMonth,
    int NewThisWeek,
    decimal SubscriptionCoverage,   // % of companies with active subscription
    
    // Status Distribution
    CompanyStatusDistributionDto StatusDistribution,
    List<DistributionItemDto> StatusChart,
    
    // Growth Analysis
    CompanyGrowthSummaryDto Growth,
    
    // Top Companies
    TopCompaniesDto TopCompanies,
    
    // Companies Needing Attention
    List<CompanyAlertDto> Alerts,
    int CriticalAlertCount,
    int HighAlertCount,
    int MediumAlertCount,
    
    // Subscription Coverage Breakdown
    int WithActiveSubscription,
    int WithTrialSubscription,
    int WithExpiredSubscription,
    int WithNoSubscription,
    
    // Timestamp
    DateTime GeneratedAt
);

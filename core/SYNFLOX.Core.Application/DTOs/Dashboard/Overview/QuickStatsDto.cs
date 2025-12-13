namespace Application.DTOs.Dashboard.Overview;

/// <summary>
/// Quick statistics for dashboard overview
/// </summary>
public record QuickStatsDto(
    // Company stats
    int TotalCompanies,
    int ActiveCompanies,        // Has active/trial subscription
    int InactiveCompanies,      // No subscription or all expired
    int CompaniesWithoutSub,    // Never had a subscription
    
    // Subscription stats
    int TotalSubscriptions,
    int ActiveSubscriptions,
    int TrialSubscriptions,
    int ExpiredSubscriptions,
    int SuspendedSubscriptions,
    
    // Expiry alerts
    int ExpiringToday,
    int ExpiringThisWeek,       // Within 7 days
    int ExpiringThisMonth,      // Within 30 days
    
    // Admin stats
    int TotalAdmins,
    int ActiveAdmins,           // Logged in within 30 days
    
    // Growth stats (compared to last month)
    decimal CompanyGrowthRate,
    decimal SubscriptionGrowthRate
);

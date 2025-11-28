namespace Application.DTOs.Dashboard.Alerts;

/// <summary>
/// Alert priority levels
/// </summary>
public enum AlertPriority
{
    Critical,   // Red - Immediate action required
    High,       // Orange - Action required soon
    Medium,     // Yellow - Should be addressed
    Low         // Blue - Informational
}

/// <summary>
/// Alert category types
/// </summary>
public enum AlertCategory
{
    SubscriptionExpiry,
    SubscriptionStatus,
    CompanyStatus,
    AdminActivity,
    SystemHealth,
    Revenue
}

namespace Application.DTOs.Dashboard.Companies;

/// <summary>
/// Company status enumeration for dashboard
/// </summary>
public enum CompanyDashboardStatus
{
    Active,         // Has active/trial subscription
    Inactive,       // No subscription or all expired
    Suspended,      // Has suspended subscription
    AtRisk          // Subscription expiring within 30 days
}

/// <summary>
/// Company status distribution for charts
/// </summary>
public record CompanyStatusDistributionDto(
    int Active,
    int Inactive,
    int Suspended,
    int AtRisk,
    int Total
);

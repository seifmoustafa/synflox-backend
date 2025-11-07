namespace Application.DTOs.Dashboard;

/// <summary>
/// System-wide statistics for the dashboard
/// </summary>
public class SystemStatisticsDto
{
    /// <summary>
    /// Total number of companies
    /// </summary>
    public int TotalCompanies { get; set; }

    /// <summary>
    /// Total number of admins
    /// </summary>
    public int TotalAdmins { get; set; }

    /// <summary>
    /// Total number of admin types
    /// </summary>
    public int TotalAdminTypes { get; set; }

    /// <summary>
    /// License status breakdown
    /// </summary>
    public LicenseStatusStatsDto LicenseStatusStats { get; set; } = new();

    /// <summary>
    /// Active admins count
    /// </summary>
    public int ActiveAdmins { get; set; }

    /// <summary>
    /// Inactive admins count
    /// </summary>
    public int InactiveAdmins { get; set; }

    /// <summary>
    /// Companies expiring in the next 30 days
    /// </summary>
    public int CompaniesExpiringSoon { get; set; }

    /// <summary>
    /// Recently created companies (last 7 days)
    /// </summary>
    public int RecentlyCreatedCompanies { get; set; }

    /// <summary>
    /// Recently created admins (last 7 days)
    /// </summary>
    public int RecentlyCreatedAdmins { get; set; }
}


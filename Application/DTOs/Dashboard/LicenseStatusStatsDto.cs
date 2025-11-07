namespace Application.DTOs.Dashboard;

/// <summary>
/// License status statistics breakdown
/// </summary>
public class LicenseStatusStatsDto
{
    /// <summary>
    /// Number of active licenses
    /// </summary>
    public int Active { get; set; }

    /// <summary>
    /// Number of expired licenses
    /// </summary>
    public int Expired { get; set; }

    /// <summary>
    /// Number of suspended licenses
    /// </summary>
    public int Suspended { get; set; }

    /// <summary>
    /// Total licenses (sum of all statuses)
    /// </summary>
    public int Total => Active + Expired + Suspended;
}


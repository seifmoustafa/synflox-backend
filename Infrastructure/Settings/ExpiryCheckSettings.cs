using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

/// <summary>
/// Settings for the subscription expiry check background service.
/// </summary>
public class ExpiryCheckSettings
{
    /// <summary>
    /// Interval in hours between expiry checks. Default is 24 hours (daily).
    /// </summary>
    [Range(1, 168)] // 1 hour to 1 week
    public int CheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// Time of day to run the check (HH:mm format). Default is midnight (00:00).
    /// </summary>
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Time must be in HH:mm format")]
    public string CheckTime { get; set; } = "00:00";

    /// <summary>
    /// Whether the expiry check service is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}


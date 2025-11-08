using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

/// <summary>
/// Settings for expiry notifications.
/// </summary>
public class NotificationSettings
{
    /// <summary>
    /// Days before expiry to send warning notifications.
    /// Default: 7, 3, 1 days before expiry.
    /// </summary>
    public List<int> ExpiryWarningDays { get; set; } = new List<int> { 7, 3, 1 };

    /// <summary>
    /// Whether email notifications are enabled.
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// Whether in-system notifications are enabled.
    /// </summary>
    public bool InSystemEnabled { get; set; } = true;

    /// <summary>
    /// Time of day to run the notification check (HH:mm format). Default is 9 AM.
    /// </summary>
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Time must be in HH:mm format")]
    public string CheckTime { get; set; } = "09:00";

    /// <summary>
    /// Whether the notification service is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}


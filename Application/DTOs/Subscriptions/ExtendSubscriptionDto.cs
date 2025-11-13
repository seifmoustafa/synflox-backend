using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// DTO for extending subscription expiry date
/// </summary>
public class ExtendSubscriptionDto
{
    /// <summary>
    /// Number of days to extend the subscription
    /// </summary>
    [Range(1, 3650)] // Max 10 years
    public int ExtensionDays { get; set; }

    /// <summary>
    /// Reason for the extension
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Whether to send email notification to the company
    /// </summary>
    public bool SendEmailNotification { get; set; } = true;

    /// <summary>
    /// Additional notes or comments
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

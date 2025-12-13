using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// DTO for subscription actions that require a reason
/// </summary>
public class SubscriptionActionDto
{
    /// <summary>
    /// Reason for the action (optional)
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Additional notes or comments (optional)
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether to send email notification to the company
    /// </summary>
    public bool SendEmailNotification { get; set; } = true;
}

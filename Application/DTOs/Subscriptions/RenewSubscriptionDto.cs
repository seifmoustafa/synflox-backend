using System;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// DTO for renewing a subscription.
/// Strategy: Updates existing subscription record, tracks all changes in SubscriptionHistory.
/// </summary>
public class RenewSubscriptionDto
{
    /// <summary>
    /// Optional: Override AutoRenew setting
    /// </summary>
    public bool? NewAutoRenew { get; set; }

    /// <summary>
    /// Optional: Reason for renewal (stored in history)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Optional: Schedule a different plan for next renewal
    /// </summary>
    public Guid? NextPlanId { get; set; }

    public DateTime? NextPlanStartDateUtc { get; set; }
}

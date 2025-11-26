using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Tracks all changes and actions performed on subscriptions
/// </summary>
public class SubscriptionHistory : AuditEntity<Guid>
{
    /// <summary>
    /// Reference to the subscription
    /// </summary>
    public Guid SubscriptionId { get; set; }
    public virtual Subscription Subscription { get; set; } = null!;

    /// <summary>
    /// Action performed (Created, Renewed, Upgraded, Suspended, etc.)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Previous status before the action
    /// </summary>
    public LicenseStatus? PreviousStatus { get; set; }

    /// <summary>
    /// New status after the action
    /// </summary>
    public LicenseStatus? NewStatus { get; set; }

    /// <summary>
    /// Previous plan ID (for upgrades)
    /// </summary>
    public Guid? PreviousPlanId { get; set; }

    /// <summary>
    /// New plan ID (for upgrades)
    /// </summary>
    public Guid? NewPlanId { get; set; }

    /// <summary>
    /// Previous expiry date
    /// </summary>
    public DateTime? PreviousExpiryDate { get; set; }

    /// <summary>
    /// New expiry date
    /// </summary>
    public DateTime? NewExpiryDate { get; set; }

    /// <summary>
    /// Reason for the action
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Admin who performed the action
    /// </summary>
    public Guid? PerformedByAdminId { get; set; }

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }
}

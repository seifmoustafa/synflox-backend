namespace Domain.Enums;

/// <summary>
/// Represents the subscription status for history tracking.
/// Used by SubscriptionHistory to record status transitions.
/// </summary>
public enum LicenseStatus
{
    /// <summary>
    /// Subscription is active and valid.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Subscription is suspended (manually deactivated).
    /// </summary>
    Suspended = 3
}

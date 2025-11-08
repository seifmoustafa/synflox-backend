namespace Domain.Enums;

/// <summary>
/// Represents the type of notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Warning that subscription is expiring soon.
    /// </summary>
    ExpiryWarning = 1,

    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Subscription was suspended.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Subscription was activated.
    /// </summary>
    Activated = 4,

    /// <summary>
    /// Subscription was resumed.
    /// </summary>
    Resumed = 5,

    /// <summary>
    /// Subscription expiry date was extended.
    /// </summary>
    Extended = 6,

    /// <summary>
    /// General notification.
    /// </summary>
    General = 7
}


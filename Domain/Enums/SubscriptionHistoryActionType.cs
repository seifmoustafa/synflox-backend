namespace Domain.Enums;

/// <summary>
/// Represents the type of action performed on a subscription.
/// </summary>
public enum SubscriptionHistoryActionType
{
    /// <summary>
    /// Company was created.
    /// </summary>
    Created = 1,

    /// <summary>
    /// Subscription was activated.
    /// </summary>
    Activated = 2,

    /// <summary>
    /// Subscription was suspended.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Subscription was resumed.
    /// </summary>
    Resumed = 4,

    /// <summary>
    /// Subscription expiry date was extended.
    /// </summary>
    Extended = 5,

    /// <summary>
    /// Subscription expired.
    /// </summary>
    Expired = 6,

    /// <summary>
    /// Company was deleted.
    /// </summary>
    Deleted = 7,

    /// <summary>
    /// Company information was updated.
    /// </summary>
    Updated = 8
}


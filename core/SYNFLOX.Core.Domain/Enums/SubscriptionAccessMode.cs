namespace Domain.Enums;

/// <summary>
/// Defines the current access mode for an entire subscription
/// This is the global access state that affects all entitlements
/// </summary>
public enum SubscriptionAccessMode
{
    /// <summary>
    /// Default/uninitialized value
    /// </summary>
    None = 0,

    /// <summary>
    /// Full access to all entitled features
    /// Normal operating mode for active subscriptions
    /// </summary>
    Full = 1,

    /// <summary>
    /// Grace period - full access but subscription is expiring soon
    /// Customer has X days to renew before transitioning to restricted access
    /// </summary>
    GracePeriod = 2,

    /// <summary>
    /// Read-only access - view and export only
    /// Customer can view and export data but cannot create, update, or delete
    /// Used when subscription expires and has a fallback plan
    /// </summary>
    ReadOnly = 3,

    /// <summary>
    /// Export-only access - data export before full block
    /// Customer has limited time (typically 30 days) to export data
    /// Last chance before complete access loss
    /// </summary>
    ExportOnly = 4,

    /// <summary>
    /// Completely blocked - upgrade required
    /// No access to any features, customer must renew/upgrade
    /// Data is retained but inaccessible
    /// </summary>
    Blocked = 5
}

namespace Domain.Enums;

/// <summary>
/// Defines the access level for an entitlement
/// Controls what operations are allowed on the entitled resource
/// </summary>
public enum EntitlementAccessLevel
{
    /// <summary>
    /// Default/uninitialized value - no access
    /// </summary>
    None = 0,

    /// <summary>
    /// Full access - all CRUD operations allowed
    /// Create, Read, Update, Delete, and Export are all permitted
    /// </summary>
    Full = 1,

    /// <summary>
    /// Read-only access - view and export only
    /// Can view data and export, but cannot create, update, or delete
    /// Typically used for expired subscriptions with fallback plan
    /// </summary>
    ReadOnly = 2,

    /// <summary>
    /// Export-only access - can only export data
    /// Last chance to extract data before complete block
    /// Temporary state before transitioning to Blocked
    /// </summary>
    ExportOnly = 3,

    /// <summary>
    /// Blocked - no access whatsoever
    /// Used when subscription is completely expired with no fallback
    /// </summary>
    Blocked = 4
}

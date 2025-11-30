namespace Domain.Enums;

/// <summary>
/// Defines the source/origin of an entitlement for audit trail purposes
/// </summary>
public enum EntitlementSource
{
    /// <summary>
    /// Default/uninitialized value
    /// </summary>
    None = 0,

    /// <summary>
    /// Inherited from subscription plan when subscription was created
    /// These are the default entitlements that come with the plan
    /// </summary>
    PlanDefault = 1,

    /// <summary>
    /// Manually granted by an administrator
    /// Used for custom access grants outside of plan defaults
    /// </summary>
    AdminGrant = 2,

    /// <summary>
    /// Added during a subscription upgrade
    /// When customer upgrades, new entitlements are marked with this source
    /// </summary>
    Upgrade = 3,

    /// <summary>
    /// Trial or promotional access (typically time-limited)
    /// Used for temporary access to features for evaluation
    /// </summary>
    Promotional = 4,

    /// <summary>
    /// Custom contract override
    /// Used for enterprise customers with special agreements
    /// </summary>
    ContractOverride = 5,

    /// <summary>
    /// Fallback/free tier access (after subscription expiry)
    /// Granted when subscription expires and falls back to free tier
    /// </summary>
    Fallback = 6
}

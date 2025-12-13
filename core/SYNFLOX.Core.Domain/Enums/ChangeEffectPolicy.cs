namespace Domain.Enums;

/// <summary>
/// Defines when a plan or subscription change takes effect.
/// Used for billing cycle-aware change management.
/// </summary>
public enum ChangeEffectPolicy
{
    /// <summary>
    /// Change applies immediately on next API call.
    /// Used for: feature additions, access upgrades, device limit increases.
    /// </summary>
    Immediate = 0,
    
    /// <summary>
    /// Change applies at the start of the next billing cycle.
    /// Used for: price changes, feature removals, downgrades.
    /// </summary>
    NextBillingCycle = 1,
    
    /// <summary>
    /// Change applies after the grace period ends.
    /// Used for: subscription expiry transitions.
    /// </summary>
    GracePeriodEnd = 2,
    
    /// <summary>
    /// Admin manually decides when the change takes effect.
    /// Used for: custom/exceptional cases.
    /// </summary>
    Manual = 3
}

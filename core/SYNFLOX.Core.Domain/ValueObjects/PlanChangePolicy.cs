using Domain.Enums;

namespace Domain.ValueObjects;

/// <summary>
/// Defines when different types of plan changes take effect.
/// Follows enterprise SaaS standards (Salesforce, Microsoft, etc.)
/// </summary>
public class PlanChangePolicy
{
    /// <summary>
    /// When price increases take effect.
    /// Default: NextBillingCycle (customer has paid for current period).
    /// </summary>
    public ChangeEffectPolicy PriceIncrease { get; set; } = ChangeEffectPolicy.NextBillingCycle;
    
    /// <summary>
    /// When price decreases take effect.
    /// Default: Immediate (customer benefit).
    /// </summary>
    public ChangeEffectPolicy PriceDecrease { get; set; } = ChangeEffectPolicy.Immediate;
    
    /// <summary>
    /// When new features/modules are added to the plan.
    /// Default: Immediate (customer benefit).
    /// </summary>
    public ChangeEffectPolicy FeatureAddition { get; set; } = ChangeEffectPolicy.Immediate;
    
    /// <summary>
    /// When features/modules are removed from the plan.
    /// Default: NextBillingCycle (customer paid for current access).
    /// </summary>
    public ChangeEffectPolicy FeatureRemoval { get; set; } = ChangeEffectPolicy.NextBillingCycle;
    
    /// <summary>
    /// When device limit is increased.
    /// Default: Immediate (customer benefit).
    /// </summary>
    public ChangeEffectPolicy DeviceLimitIncrease { get; set; } = ChangeEffectPolicy.Immediate;
    
    /// <summary>
    /// When device limit is decreased.
    /// Default: NextBillingCycle (customer may already be using devices).
    /// </summary>
    public ChangeEffectPolicy DeviceLimitDecrease { get; set; } = ChangeEffectPolicy.NextBillingCycle;
    
    /// <summary>
    /// When access level is upgraded (e.g., ReadOnly → Full).
    /// Default: Immediate (customer benefit).
    /// </summary>
    public ChangeEffectPolicy AccessUpgrade { get; set; } = ChangeEffectPolicy.Immediate;
    
    /// <summary>
    /// When access level is downgraded (e.g., Full → ReadOnly).
    /// Default: NextBillingCycle (customer paid for current access).
    /// </summary>
    public ChangeEffectPolicy AccessDowngrade { get; set; } = ChangeEffectPolicy.NextBillingCycle;
    
    /// <summary>
    /// When a plan upgrade takes effect.
    /// Default: Immediate (customer benefit).
    /// </summary>
    public ChangeEffectPolicy PlanUpgrade { get; set; } = ChangeEffectPolicy.Immediate;
    
    /// <summary>
    /// When a plan downgrade takes effect.
    /// Default: NextBillingCycle (customer paid for current plan).
    /// </summary>
    public ChangeEffectPolicy PlanDowngrade { get; set; } = ChangeEffectPolicy.NextBillingCycle;
    
    /// <summary>
    /// Default policy used by all plans unless overridden.
    /// </summary>
    public static PlanChangePolicy Default => new();
}

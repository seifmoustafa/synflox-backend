using System;

namespace Domain.ValueObjects;

/// <summary>
/// Represents billing cycle information for a subscription.
/// Used to determine when plan changes take effect.
/// </summary>
public class BillingCycle
{
    /// <summary>
    /// Start date of the current billing cycle.
    /// </summary>
    public DateTime StartDateUtc { get; set; }
    
    /// <summary>
    /// End date of the current billing cycle.
    /// </summary>
    public DateTime EndDateUtc { get; set; }
    
    /// <summary>
    /// Billing period type.
    /// </summary>
    public BillingPeriod Period { get; set; }
    
    /// <summary>
    /// Days remaining in the current billing cycle.
    /// </summary>
    public int DaysRemaining => Math.Max(0, (int)(EndDateUtc - DateTime.UtcNow).TotalDays);
    
    /// <summary>
    /// Whether the billing cycle has ended.
    /// </summary>
    public bool HasEnded => DateTime.UtcNow >= EndDateUtc;
    
    /// <summary>
    /// Percentage of the billing cycle that has elapsed.
    /// </summary>
    public double PercentageElapsed
    {
        get
        {
            var totalDays = (EndDateUtc - StartDateUtc).TotalDays;
            if (totalDays <= 0) return 100;
            var elapsedDays = (DateTime.UtcNow - StartDateUtc).TotalDays;
            return Math.Min(100, Math.Max(0, (elapsedDays / totalDays) * 100));
        }
    }
    
    /// <summary>
    /// Calculate the next billing cycle start date.
    /// </summary>
    public DateTime GetNextCycleStartDate()
    {
        return Period switch
        {
            BillingPeriod.Monthly => EndDateUtc,
            BillingPeriod.Quarterly => EndDateUtc,
            BillingPeriod.SemiAnnually => EndDateUtc,
            BillingPeriod.Annually => EndDateUtc,
            BillingPeriod.Lifetime => DateTime.MaxValue,
            _ => EndDateUtc
        };
    }
}

/// <summary>
/// Billing period types.
/// </summary>
public enum BillingPeriod
{
    Monthly = 0,
    Quarterly = 1,
    SemiAnnually = 2,
    Annually = 3,
    Lifetime = 4
}

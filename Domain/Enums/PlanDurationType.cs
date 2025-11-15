namespace Domain.Enums;

/// <summary>
/// Defines the duration type for subscription plans
/// </summary>
public enum PlanDurationType
{
    /// <summary>
    /// 7-day subscription period
    /// </summary>
    Weekly = 1,

    /// <summary>
    /// 14-day subscription period (bi-weekly)
    /// </summary>
    BiWeekly = 2,

    /// <summary>
    /// 1-month subscription period
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// 3-month subscription period
    /// </summary>
    Quarterly = 4,

    /// <summary>
    /// 6-month subscription period
    /// </summary>
    SemiAnnually = 5,

    /// <summary>
    /// 12-month subscription period
    /// </summary>
    Yearly = 6,

    /// <summary>
    /// 2-year subscription period
    /// </summary>
    Biennial = 7,

    /// <summary>
    /// 3-year subscription period
    /// </summary>
    Triennial = 8,

    /// <summary>
    /// Permanent subscription - never expires unless manually stopped
    /// Cannot auto-renew, cannot schedule upgrades
    /// </summary>
    Lifetime = 99
}

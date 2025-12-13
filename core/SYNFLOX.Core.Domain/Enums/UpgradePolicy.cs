namespace Domain.Enums;

/// <summary>
/// Defines how subscription upgrades are handled
/// </summary>
public enum UpgradePolicy
{
    /// <summary>
    /// Immediately terminate current subscription and start new one with full duration
    /// </summary>
    FullReplace = 0,

    /// <summary>
    /// Terminate current subscription and start new one immediately with prorated billing
    /// </summary>
    Prorated = 1,

    /// <summary>
    /// Schedule the upgrade to start after current subscription expires
    /// </summary>
    Deferred = 2
}

namespace Domain.Enums;

/// <summary>
/// Represents the subscription status of a company.
/// </summary>
public enum LicenseStatus
{
    /// <summary>
    /// Company is active and subscription is valid.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Company subscription is suspended (manually deactivated).
    /// </summary>
    Suspended = 3
}


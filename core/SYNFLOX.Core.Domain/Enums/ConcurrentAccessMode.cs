namespace Domain.Enums;

/// <summary>
/// Defines how concurrent device access is handled for a subscription plan.
/// Controls whether multiple devices can use the license simultaneously.
/// </summary>
public enum ConcurrentAccessMode
{
    /// <summary>
    /// Only ONE device can be active at a time.
    /// New device login automatically kicks the previous device.
    /// Best for: Single-user licenses, strict control.
    /// </summary>
    SingleDevice = 1,

    /// <summary>
    /// Up to N devices can be active simultaneously.
    /// Use MaxConcurrentDevices to set the limit.
    /// Best for: Team licenses with seat limits.
    /// </summary>
    LimitedConcurrent = 2,

    /// <summary>
    /// All allowed devices (up to MaxDevices) can access simultaneously.
    /// No concurrent usage restrictions.
    /// Best for: Enterprise licenses, unlimited access.
    /// </summary>
    Unlimited = 3,

    /// <summary>
    /// All devices can access, but only during configured time windows.
    /// Uses AccessTimeWindow records to define allowed periods.
    /// Best for: Shift-based access, business hours only.
    /// </summary>
    TimeBasedUnlimited = 4,

    /// <summary>
    /// Up to N devices can access during configured time windows.
    /// Combines concurrent limits with time restrictions.
    /// Best for: Controlled access during specific hours.
    /// </summary>
    TimeBasedLimited = 5
}

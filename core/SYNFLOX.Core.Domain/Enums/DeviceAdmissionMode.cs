namespace Domain.Enums;

/// <summary>
/// Defines how devices are admitted/registered for a subscription.
/// Controls whether devices can self-register or require admin approval.
/// </summary>
public enum DeviceAdmissionMode
{
    /// <summary>
    /// Any device can register freely up to the device limit.
    /// Best for: Unlimited plans, self-service scenarios.
    /// When limit reached: New devices blocked until admin unbinds existing ones.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Company admin must explicitly bind each device.
    /// Devices cannot self-register.
    /// Best for: High-security environments, strict device control.
    /// </summary>
    AdminOnly = 2,

    /// <summary>
    /// Devices auto-register up to the limit.
    /// When limit reached, new devices go to pending queue for admin approval.
    /// Best for: Balanced approach - convenience with oversight.
    /// </summary>
    AutoWithQueue = 3,

    /// <summary>
    /// First N devices auto-register, rest require admin approval.
    /// Uses MaxAutoAdmitDevices to set the auto-admit threshold.
    /// Best for: Teams with core members + occasional guests.
    /// </summary>
    HybridAutoAdmin = 4
}

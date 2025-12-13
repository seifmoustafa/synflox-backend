namespace Domain.Enums;

/// <summary>
/// Status of an online device binding.
/// </summary>
public enum OnlineDeviceStatus
{
    /// <summary>
    /// Device is actively registered and can access the system.
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Device is temporarily suspended (e.g., suspicious activity).
    /// </summary>
    Suspended = 1,
    
    /// <summary>
    /// Device registration has been revoked.
    /// </summary>
    Revoked = 2,
    
    /// <summary>
    /// Device is pending approval (for managed device policies).
    /// </summary>
    PendingApproval = 3
}

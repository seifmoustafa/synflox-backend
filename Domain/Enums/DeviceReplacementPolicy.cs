namespace Domain.Enums;

/// <summary>
/// Policy for handling device replacement when maximum devices reached.
/// </summary>
public enum DeviceReplacementPolicy
{
    /// <summary>
    /// Client admin must manually approve replacement.
    /// Most secure option - requires explicit action.
    /// </summary>
    AdminApproval = 0,
    
    /// <summary>
    /// Automatically replace the oldest activated device.
    /// Based on ActivatedAtUtc timestamp.
    /// </summary>
    AutoReplaceOldest = 1,
    
    /// <summary>
    /// Automatically replace the least recently active device.
    /// Based on LastSeenAtUtc timestamp (least recently used).
    /// </summary>
    AutoReplaceLeastActive = 2,
    
    /// <summary>
    /// Block new activations entirely until admin unbinds a device.
    /// Most restrictive option.
    /// </summary>
    BlockNewActivations = 3
}

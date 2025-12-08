namespace Domain.Enums;

/// <summary>
/// Defines the type of client licensing system.
/// Used to distinguish between online and offline access patterns.
/// </summary>
public enum ClientSystemType
{
    /// <summary>
    /// Online client - uses thin JWT tokens and fetches entitlements via API.
    /// Real-time updates, no token regeneration needed for plan changes.
    /// </summary>
    Online = 0,
    
    /// <summary>
    /// Offline client - uses self-contained encrypted license keys.
    /// Requires license key regeneration when entitlements change.
    /// </summary>
    Offline = 1
}

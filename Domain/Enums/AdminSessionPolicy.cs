namespace Domain.Enums;

/// <summary>
/// Defines how client admin sessions are managed.
/// Configurable by the admin for their preferred security level.
/// </summary>
public enum AdminSessionPolicy
{
    /// <summary>
    /// Only one active session allowed at a time.
    /// New login automatically terminates the previous session.
    /// Most secure option - recommended default.
    /// </summary>
    SingleSession = 1,

    /// <summary>
    /// Multiple sessions allowed, but admin is warned on login
    /// if other active sessions exist.
    /// Balanced security and convenience.
    /// </summary>
    MultipleWithWarning = 2,

    /// <summary>
    /// No restrictions on concurrent sessions.
    /// Admin can be logged in from multiple devices simultaneously.
    /// Least secure but most flexible.
    /// </summary>
    MultipleUnlimited = 3
}

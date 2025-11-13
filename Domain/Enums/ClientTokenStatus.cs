namespace Domain.Enums;

/// <summary>
/// Status of a client access token
/// </summary>
public enum ClientTokenStatus
{
    /// <summary>
    /// Token is active and can be used
    /// </summary>
    Active = 1,

    /// <summary>
    /// Token has expired naturally
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Token has been manually revoked by admin
    /// </summary>
    Revoked = 3,

    /// <summary>
    /// Token is suspended due to subscription issues
    /// </summary>
    Suspended = 4,

    /// <summary>
    /// Token is pending activation (future use)
    /// </summary>
    Pending = 5
}

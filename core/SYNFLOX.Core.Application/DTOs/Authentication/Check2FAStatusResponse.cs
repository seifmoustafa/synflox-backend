namespace Application.DTOs.Authentication;

/// <summary>
/// Response indicating if an email has 2FA enabled
/// Used in forgot password flow to determine if 2FA verification is needed
/// </summary>
public class Check2FAStatusResponse
{
    /// <summary>
    /// True if the email address has 2FA enabled
    /// </summary>
    public bool Has2FA { get; set; }

    /// <summary>
    /// True if the email exists in the system
    /// Always returns true for security (don't leak user existence)
    /// </summary>
    public bool EmailExists { get; set; } = true;
}

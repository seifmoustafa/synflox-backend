namespace Application.DTOs.Authentication;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to initiate password reset with 2FA verification
/// Required when user has 2FA enabled
/// Either TwoFactorCode OR BackupCode must be provided
/// </summary>
public class ForgotPasswordWith2FARequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 6-digit 2FA code from authenticator app (required if 2FA enabled and not using backup code)
    /// </summary>
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// 8-character backup code (alternative to 2FA code if user lost authenticator)
    /// </summary>
    public string? BackupCode { get; set; }
}

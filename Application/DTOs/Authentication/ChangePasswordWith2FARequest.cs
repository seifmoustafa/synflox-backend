namespace Application.DTOs.Authentication;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to change password with 2FA verification
/// Either TwoFactorCode OR BackupCode must be provided if 2FA is enabled
/// </summary>
public class ChangePasswordWith2FARequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]+$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character (@$!%*?&#)")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 6-digit 2FA code from authenticator app (required if 2FA enabled and not using backup code)
    /// </summary>
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// 8-character backup code (alternative to 2FA code if user lost authenticator)
    /// </summary>
    public string? BackupCode { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication;

public class Verify2FARequest
{
    [Required]
    public required string Username { get; set; }

    [Required]
    public required string Password { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "2FA code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "2FA code must be 6 digits")]
    public required string TwoFactorCode { get; set; }
}

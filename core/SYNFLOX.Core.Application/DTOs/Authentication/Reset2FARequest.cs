using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request to reset two-factor authentication
    /// Generates new secret and invalidates old one
    /// Requires current password confirmation for security
    /// </summary>
    public class Reset2FARequest
    {
        /// <summary>
        /// Current password for verification
        /// Required to prevent unauthorized 2FA reset
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Current password is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 100 characters")]
        public required string CurrentPassword { get; set; }
    }
}

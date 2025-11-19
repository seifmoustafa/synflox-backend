using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request to verify backup code for 2FA
    /// Used when user lost their authenticator app
    /// </summary>
    public class VerifyBackupCodeRequest
    {
        /// <summary>
        /// Admin username or email
        /// </summary>
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100, ErrorMessage = "Username must not exceed 100 characters")]
        public required string Username { get; set; }

        /// <summary>
        /// 8-character backup code (alphanumeric, case-insensitive)
        /// Will be converted to uppercase during verification
        /// </summary>
        [Required(ErrorMessage = "Backup code is required")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "Backup code must be exactly 8 characters")]
        [RegularExpression(@"^[A-Za-z0-9]{8}$", ErrorMessage = "Backup code must be 8 alphanumeric characters")]
        public required string BackupCode { get; set; }
    }
}

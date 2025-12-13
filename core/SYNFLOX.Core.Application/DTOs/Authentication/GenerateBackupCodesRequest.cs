using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request to generate new set of backup codes
    /// SECURITY: Requires current password confirmation
    /// </summary>
    public class GenerateBackupCodesRequest
    {
        /// <summary>
        /// Current password for security confirmation
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 100 characters")]
        public required string CurrentPassword { get; set; }
    }
}

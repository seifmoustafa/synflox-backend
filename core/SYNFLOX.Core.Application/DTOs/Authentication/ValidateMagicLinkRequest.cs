using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request to validate magic link token for password reset
    /// </summary>
    public class ValidateMagicLinkRequest
    {
        /// <summary>
        /// Encrypted magic link token from email
        /// </summary>
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;
    }
}

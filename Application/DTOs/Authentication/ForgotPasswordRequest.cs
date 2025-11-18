using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request to initiate password reset process
    /// </summary>
    public class ForgotPasswordRequest
    {
        /// <summary>
        /// Email address of the admin requesting password reset
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        public required string Email { get; set; }
    }
}

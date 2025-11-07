using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    public class PasswordResetOtpRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;
    }
}

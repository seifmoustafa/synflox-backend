using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    public class ResendEmailVerificationOtpRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;
    }
}

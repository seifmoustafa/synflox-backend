using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    public class EmailVerificationRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}

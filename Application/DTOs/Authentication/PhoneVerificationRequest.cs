using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    public class PhoneVerificationRequest
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}

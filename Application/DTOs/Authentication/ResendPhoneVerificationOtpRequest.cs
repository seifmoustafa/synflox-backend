using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request payload for resending a phone verification OTP.
    /// </summary>
    public class ResendPhoneVerificationOtpRequest
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}


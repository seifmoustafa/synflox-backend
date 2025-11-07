using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request payload for sending a phone verification OTP.
    /// </summary>
    public class SendPhoneVerificationOtpRequest
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Response containing validated magic link data
    /// </summary>
    public class MagicLinkValidationResponse
    {
        /// <summary>
        /// Admin email for password reset
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Validated OTP code (auto-filled for frontend)
        /// </summary>
        public string OtpCode { get; set; } = string.Empty;

        /// <summary>
        /// Whether token is valid and not expired
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Remaining minutes before expiry
        /// </summary>
        public int ExpiryMinutes { get; set; }
    }
}

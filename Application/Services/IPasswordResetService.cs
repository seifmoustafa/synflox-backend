using System.Threading.Tasks;
using Application.DTOs.Authentication;

namespace Application.Services
{
    /// <summary>
    /// Service for handling password reset operations
    /// </summary>
    public interface IPasswordResetService
    {
        /// <summary>
        /// Send password reset OTP to admin's email
        /// Rate limit: Max 3 requests per hour per email
        /// </summary>
        /// <param name="request">Email address</param>
        /// <param name="ipAddress">IP address of requester (for logging)</param>
        /// <returns>Success message</returns>
        Task<string> SendPasswordResetOtpAsync(ForgotPasswordRequest request, string? ipAddress = null);

        /// <summary>
        /// Verify OTP code for password reset
        /// Max 5 attempts allowed
        /// </summary>
        /// <param name="request">Email and OTP code</param>
        /// <returns>Success message</returns>
        Task<string> VerifyResetOtpAsync(VerifyResetOtpRequest request);

        /// <summary>
        /// Reset password after OTP verification
        /// Invalidates all refresh tokens for security
        /// </summary>
        /// <param name="request">Email, OTP, and new password</param>
        /// <returns>Success message</returns>
        Task<string> ResetPasswordAsync(ResetPasswordRequest request);

        /// <summary>
        /// Validate magic link token from email and return OTP for auto-fill
        /// Used when admin clicks magic link in email instead of manual OTP entry
        /// </summary>
        /// <param name="request">Encrypted magic link token</param>
        /// <returns>Validation response with email and OTP</returns>
        Task<MagicLinkValidationResponse> ValidateMagicLinkAsync(ValidateMagicLinkRequest request);
    }
}

using System;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Application.DTOs.Admin;

namespace Application.Services
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates an administrator using username and password.
        /// </summary>
        Task<AuthenticationResponse> AdminAuthenticationAsync(string username, string password);

        /// <summary>
        /// Registers an administrator account.
        /// </summary>
        Task<AdminDto> RegisterAdminAsync(CreateAdminDto request);

        /// <summary>
        /// Regenerates access token using refresh token.
        /// </summary>
        Task<AuthenticationResponse> RegenerateAccessToken(RefreshTokenRequest request);

        /// <summary>
        /// Logs out an admin by revoking refresh token.
        /// </summary>
        Task Logout(Guid adminId);

        /// <summary>
        /// Changes the password of another administrator.
        /// </summary>
        Task ChangeAdminPasswordAsync(ChangePasswordByIdRequest request);

        /// <summary>
        /// Verifies 2FA code and completes login for admins with 2FA enabled.
        /// </summary>
        Task<AuthenticationResponse> Verify2FAAsync(string username, string password, string twoFactorCode);

        /// <summary>
        /// Check if an email address has 2FA enabled
        /// Used in forgot password flow to determine if 2FA verification is required
        /// Returns false for non-existent emails (don't leak user existence)
        /// </summary>
        Task<Check2FAStatusResponse> Check2FAStatusAsync(string email);
    }
}

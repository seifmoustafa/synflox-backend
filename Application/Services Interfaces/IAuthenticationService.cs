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
        /// Regenerates access token using refresh token (for Admin).
        /// </summary>
        Task<AuthenticationResponse> RegenerateAccessToken(Guid adminId);

        /// <summary>
        /// Regenerates access token using a refresh token string (no access token required).
        /// </summary>
        Task<AuthenticationResponse> RefreshWithTokenAsync(string refreshToken);

        /// <summary>
        /// Logs out an admin by revoking refresh token.
        /// </summary>
        Task Logout(Guid adminId);

        /// <summary>
        /// Changes the password of another administrator.
        /// </summary>
        Task ChangeAdminPasswordAsync(Guid adminId, string newPassword);
    }
}

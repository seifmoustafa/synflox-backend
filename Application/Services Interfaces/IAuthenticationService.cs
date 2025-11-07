using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Application.DTOs.User;
using Application.DTOs.Admin;

namespace Application.Services
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user using email, phone or username and password.
        /// </summary>
        Task<AuthenticationResponse> AuthenticationAsync(string credential, string password);
        /// <summary>
        /// Authenticates an administrator using username and password.
        /// </summary>
        Task<AuthenticationResponse> AdminAuthenticationAsync(string username, string password);

        /// <summary>
        /// Registers a regular user.
        /// </summary>
        Task<UserDto> RegisterAsync(RegistrationRequest request);

        /// <summary>
        /// Registers an administrator account.
        /// </summary>
        Task<AdminDto> RegisterAdminAsync(CreateAdminDto request);

        Task<UserDto> UpdateUserAsync(Guid userId, UpdateProfileRequest request);

        Task<AuthenticationResponse> ExternalLoginAsync(string provider, ExternalAuthRequest request);

        Task<AuthenticationResponse> RegenerateAccessToken(Guid userId);

        Task Logout(Guid userId);

        /// <summary>
        /// Changes the password of another administrator.
        /// </summary>
        Task ChangeAdminPasswordAsync(Guid adminId, string newPassword);

        /// <summary>
        /// Changes the password for the specified user.
        /// </summary>
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

        Task<OtpSendResult> SendPasswordResetCodeAsync(string email);
        Task ResetPasswordWithCodeAsync(string email, string code, string newPassword);
        Task<OtpSendResult> ResendVerificationCodeAsync(string email);
        Task VerifyEmailAsync(string email, string code);
        Task<OtpSendResult> SendPhoneVerificationCodeAsync(string phoneNumber);
        Task<OtpSendResult> ResendPhoneVerificationCodeAsync(string phoneNumber);
        Task VerifyPhoneAsync(string phoneNumber, string code);

        Task<UserDto> ChangeEmailAsync(Guid userId, string email);
        Task<UserDto> ChangePhoneAsync(Guid userId, string phoneNumber);
        Task<UserDto> ChangeUsernameAsync(Guid userId, string username);
        Task DeleteUserImageAsync(Guid userId);
    }
}

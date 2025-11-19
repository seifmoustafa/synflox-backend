using System;
using System.Threading.Tasks;
using Application.DTOs.Authentication;

namespace Application.Services
{
    /// <summary>
    /// Service for managing backup codes for 2FA recovery
    /// </summary>
    public interface IBackupCodeService
    {
        /// <summary>
        /// Generate new set of 10 backup codes for an admin
        /// Invalidates all previous codes
        /// Codes are SHA256 hashed before storage
        /// SECURITY: Requires current password confirmation
        /// </summary>
        /// <param name="adminId">Admin ID</param>
        /// <param name="currentPassword">Current password for confirmation</param>
        /// <returns>Response with plain text codes (shown only once)</returns>
        Task<GenerateBackupCodesResponse> GenerateBackupCodesAsync(Guid adminId, string currentPassword);

        /// <summary>
        /// Verify backup code for 2FA authentication and generate JWT token
        /// Marks code as used after successful verification
        /// Returns JWT access token and refresh token for login
        /// </summary>
        /// <param name="request">Username and backup code</param>
        /// <returns>Authentication response with JWT tokens</returns>
        Task<AuthenticationResponse> VerifyBackupCodeAsync(VerifyBackupCodeRequest request);

        /// <summary>
        /// Get status of backup codes for an admin
        /// Returns count of remaining unused codes
        /// </summary>
        /// <param name="adminId">Admin ID</param>
        /// <returns>Backup codes status</returns>
        Task<BackupCodesStatusDto> GetBackupCodesStatusAsync(Guid adminId);

        /// <summary>
        /// Delete all backup codes for an admin
        /// Called when disabling 2FA
        /// </summary>
        /// <param name="adminId">Admin ID</param>
        Task DeleteAllBackupCodesAsync(Guid adminId);
    }
}

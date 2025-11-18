using System;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository interface for PasswordResetToken operations
    /// </summary>
    public interface IPasswordResetTokenRepository : IBaseRepository<Guid, PasswordResetToken>
    {
        /// <summary>
        /// Get the most recent valid token for an admin
        /// </summary>
        Task<PasswordResetToken?> GetActiveTokenByAdminIdAsync(Guid adminId);

        /// <summary>
        /// Invalidate all existing tokens for an admin
        /// Called before creating new token or after successful password reset
        /// </summary>
        Task InvalidateAllTokensForAdminAsync(Guid adminId);

        /// <summary>
        /// Count active reset requests in the last hour for rate limiting
        /// </summary>
        Task<int> CountRecentRequestsAsync(Guid adminId, DateTime since);

        /// <summary>
        /// Clean up expired tokens (optional housekeeping)
        /// </summary>
        Task DeleteExpiredTokensAsync();
    }
}

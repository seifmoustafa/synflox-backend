using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository interface for BackupCode operations
    /// </summary>
    public interface IBackupCodeRepository : IBaseRepository<Guid, BackupCode>
    {
        /// <summary>
        /// Get all backup codes for an admin (used and unused)
        /// </summary>
        Task<List<BackupCode>> GetAllByAdminIdAsync(Guid adminId);

        /// <summary>
        /// Get count of unused backup codes for an admin
        /// </summary>
        Task<int> CountUnusedCodesAsync(Guid adminId);

        /// <summary>
        /// Get backup code by admin ID and code hash
        /// Used for validation during 2FA
        /// </summary>
        Task<BackupCode?> GetByAdminAndHashAsync(Guid adminId, string codeHash);

        /// <summary>
        /// Invalidate all backup codes for an admin
        /// Called when generating new set of codes
        /// </summary>
        Task InvalidateAllCodesForAdminAsync(Guid adminId);

        /// <summary>
        /// Delete all backup codes for an admin
        /// Called when disabling 2FA or regenerating codes
        /// </summary>
        Task DeleteAllForAdminAsync(Guid adminId);

        /// <summary>
        /// Mark a backup code as used
        /// </summary>
        Task MarkAsUsedAsync(Guid codeId);
    }
}

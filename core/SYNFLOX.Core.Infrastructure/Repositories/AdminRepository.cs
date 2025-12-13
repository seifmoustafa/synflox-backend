using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AdminRepository : BaseRepository<Guid, Admin>, IAdminRepository
{
    public AdminRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<Admin> GetByUserNameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Username == username);
    }

    #region Delete Cascade Support

    public async Task<bool> HasRelatedRecordsAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var hasTokens = await _context.RefreshTokens
            .AnyAsync(t => t.AdminId == adminId, cancellationToken);
        if (hasTokens) return true;

        var hasBackupCodes = await _context.BackupCodes
            .AnyAsync(c => c.AdminId == adminId, cancellationToken);
        if (hasBackupCodes) return true;

        var hasResetTokens = await _context.PasswordResetTokens
            .AnyAsync(t => t.AdminId == adminId, cancellationToken);
        if (hasResetTokens) return true;

        var hasAuditLogs = await _context.SecurityAuditLogs
            .AnyAsync(l => l.AdminId == adminId, cancellationToken);
        
        return hasAuditLogs;
    }

    public async Task SoftDeleteAuthRecordsAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Delete refresh tokens (hard delete - they're session tokens)
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.AdminId == adminId)
            .ToListAsync(cancellationToken);
        _context.RefreshTokens.RemoveRange(refreshTokens);

        // Delete backup codes (hard delete - security)
        var backupCodes = await _context.BackupCodes
            .Where(c => c.AdminId == adminId)
            .ToListAsync(cancellationToken);
        _context.BackupCodes.RemoveRange(backupCodes);

        // Delete password reset tokens (hard delete)
        var resetTokens = await _context.PasswordResetTokens
            .Where(t => t.AdminId == adminId)
            .ToListAsync(cancellationToken);
        _context.PasswordResetTokens.RemoveRange(resetTokens);

        // Keep audit logs but mark as orphaned (soft approach)
        var auditLogs = await _context.SecurityAuditLogs
            .Where(l => l.AdminId == adminId)
            .ToListAsync(cancellationToken);
        foreach (var log in auditLogs)
        {
            log.AdminId = null; // Orphan the log, keep for audit trail
        }
    }

    public async Task<int> GetAdminsCountByTypeAsync(Guid adminTypeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(a => a.AdminTypeId == adminTypeId && !a.IsDeleted, cancellationToken);
    }

    #endregion
}

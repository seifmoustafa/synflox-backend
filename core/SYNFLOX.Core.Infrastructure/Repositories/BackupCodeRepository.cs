using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class BackupCodeRepository : BaseRepository<Guid, BackupCode>, IBackupCodeRepository
    {
        public BackupCodeRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<List<BackupCode>> GetAllByAdminIdAsync(Guid adminId)
        {
            return await _dbSet
                .Where(c => c.AdminId == adminId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CountUnusedCodesAsync(Guid adminId)
        {
            return await _dbSet
                .Where(c => c.AdminId == adminId && !c.IsUsed)
                .CountAsync();
        }

        public async Task<BackupCode?> GetByAdminAndHashAsync(Guid adminId, string codeHash)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.AdminId == adminId && c.CodeHash == codeHash && !c.IsUsed);
        }

        public async Task InvalidateAllCodesForAdminAsync(Guid adminId)
        {
            var codes = await _dbSet
                .Where(c => c.AdminId == adminId && !c.IsUsed)
                .ToListAsync();

            foreach (var code in codes)
            {
                code.IsUsed = true;
                code.UsedAt = DateTime.UtcNow;
            }
            // SaveChanges will be called by UnitOfWork in service layer
        }

        public async Task DeleteAllForAdminAsync(Guid adminId)
        {
            var codes = await _dbSet
                .Where(c => c.AdminId == adminId)
                .ToListAsync();

            _dbSet.RemoveRange(codes);
            // SaveChanges will be called by UnitOfWork in service layer
        }

        public async Task MarkAsUsedAsync(Guid codeId)
        {
            var code = await _dbSet.FindAsync(codeId);
            if (code == null)
            {
                throw new InvalidOperationException($"Backup code with ID {codeId} not found. Cannot mark as used.");
            }

            code.IsUsed = true;
            code.UsedAt = DateTime.UtcNow;
            // SaveChanges will be called by UnitOfWork in service layer
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : BaseRepository<Guid, PasswordResetToken>, IPasswordResetTokenRepository
    {
        public PasswordResetTokenRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<PasswordResetToken?> GetActiveTokenByAdminIdAsync(Guid adminId)
        {
            return await _dbSet
                .Where(t => t.AdminId == adminId && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task InvalidateAllTokensForAdminAsync(Guid adminId)
        {
            var tokens = await _dbSet
                .Where(t => t.AdminId == adminId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> CountRecentRequestsAsync(Guid adminId, DateTime since)
        {
            return await _dbSet
                .Where(t => t.AdminId == adminId && t.CreatedAt >= since)
                .CountAsync();
        }

        public async Task DeleteExpiredTokensAsync()
        {
            var expiredTokens = await _dbSet
                .Where(t => t.ExpiresAt < DateTime.UtcNow.AddDays(-1)) // Keep 1 day for audit
                .ToListAsync();

            _dbSet.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}

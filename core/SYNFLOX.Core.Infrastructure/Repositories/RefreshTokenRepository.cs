using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RefreshTokenRepository : BaseRepository<int, RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(ApplicationDBContext context)
            : base(context) { }

        public async Task<RefreshToken?> GetByTokenAndAdminId(Guid adminId, string token)
        {
            return await _dbSet.FirstOrDefaultAsync(d =>
                d.AdminId == adminId && d.Token == token && d.IsActive
            );
        }

        public async Task<RefreshToken?> GetByAdminId(Guid adminId)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.AdminId == adminId && d.IsActive);
        }

        public async Task<RefreshToken?> GetByTokenAsync(
            string token,
            CancellationToken cancellationToken = default
        )
        {
            return await _dbSet
                .Include(t => t.CompanyAdmin)
                .FirstOrDefaultAsync(t => t.Token == token && t.IsActive, cancellationToken);
        }

        public async Task<List<RefreshToken>> GetActiveByCompanyAdminIdAsync(
            Guid companyAdminId,
            CancellationToken cancellationToken = default
        )
        {
            return await _dbSet
                .Where(t =>
                    t.CompanyAdminId == companyAdminId && t.IsActive && !t.IsRevoked && !t.IsExpired
                )
                .ToListAsync(cancellationToken);
        }
    }
}

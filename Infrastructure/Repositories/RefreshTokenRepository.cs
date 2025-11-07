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
        public RefreshTokenRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<RefreshToken?> GetByTokenAndUserId(Guid userId, string token)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.UserId == userId && d.Token == token && d.IsActive);
        }

        public async Task<RefreshToken?> GetByTokenAndAdminId(Guid adminId, string token)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.AdminId == adminId && d.Token == token && d.IsActive);

        }

        public async Task<RefreshToken?> GetByUserId(Guid userId)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);
        }
    }

}

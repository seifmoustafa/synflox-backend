using System;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class OtpRepository : BaseRepository<int, OtpCode>, IOtpRepository
    {
        public OtpRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<OtpCode?> GetValidOtpAsync(Guid userId, string code, OtpPurpose purpose)
        {
            return await _dbSet.FirstOrDefaultAsync(o => o.UserId == userId && o.Code == code &&
                o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);
        }
    }
}

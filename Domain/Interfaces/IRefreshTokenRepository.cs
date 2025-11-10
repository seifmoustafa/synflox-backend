using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Domain.Interfaces
{
    public interface IRefreshTokenRepository : IBaseRepository<int, RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAndAdminId(Guid adminId, string token);
        Task<RefreshToken?> GetByAdminId(Guid adminId);
        Task<RefreshToken?> GetByToken(string token);
    }
}

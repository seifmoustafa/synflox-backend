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
        // Admin (SYNFLOX) token operations
        Task<RefreshToken?> GetByTokenAndAdminId(Guid adminId, string token);
        Task<RefreshToken?> GetByAdminId(Guid adminId);

        // General token lookup
        Task<RefreshToken?> GetByTokenAsync(
            string token,
            CancellationToken cancellationToken = default
        );

        // CompanyAdmin token operations
        Task<List<RefreshToken>> GetActiveByCompanyAdminIdAsync(
            Guid companyAdminId,
            CancellationToken cancellationToken = default
        );
    }
}

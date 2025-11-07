using System;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Enums;

namespace Domain.Interfaces
{
    public interface IOtpRepository : IBaseRepository<int, OtpCode>
    {
        Task<OtpCode?> GetValidOtpAsync(Guid userId, string code, OtpPurpose purpose);
    }
}

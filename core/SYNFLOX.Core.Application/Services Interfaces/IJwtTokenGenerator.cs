using Domain.Entities.Authentication;
using Domain.Entities.Licensing;

namespace Application.Services
{
    public interface IJwtTokenGenerator
    {
        // Admin (SYNFLOX admin) token generation
        string GenerateToken(Admin admin);
        RefreshToken GenerateRefreshToken(Admin admin);

        // CompanyAdmin (Client portal) token generation
        string GenerateCompanyAdminToken(CompanyAdmin companyAdmin);
        RefreshToken GenerateCompanyAdminRefreshToken(CompanyAdmin companyAdmin);
    }
}

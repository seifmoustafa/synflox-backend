using Domain.Entities.Authentication;

namespace Application.Services
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Admin admin);
        RefreshToken GenerateRefreshToken(Admin admin);
    }
}

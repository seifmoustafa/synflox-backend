using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Application.Services
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
        string GenerateToken(Admin admin);

        RefreshToken GenerateRefreshToken(User user);
        RefreshToken GenerateRefreshToken(Admin admin);
    }
}

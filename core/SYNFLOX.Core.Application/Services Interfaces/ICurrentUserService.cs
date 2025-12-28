using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface ICurrentUserService
    {
        // Admin (SYNFLOX) properties
        Guid UserId { get; }
        string? AdminTypeName { get; }

        // CompanyAdmin (Client Portal) properties
        Guid CompanyAdminId { get; }
        Guid CompanyId { get; }
        string? DisplayName { get; }
        bool IsCompanyAdmin { get; }
    }
}

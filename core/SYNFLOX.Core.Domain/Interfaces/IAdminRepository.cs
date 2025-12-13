using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Domain.Interfaces;

public interface IAdminRepository : IBaseRepository<Guid, Admin>
{
    Task<Admin> GetByUserNameAsync(string username);
    
    // Delete cascade support
    Task<bool> HasRelatedRecordsAsync(Guid adminId, CancellationToken cancellationToken = default);
    Task SoftDeleteAuthRecordsAsync(Guid adminId, CancellationToken cancellationToken = default);
    
    // AdminType support
    Task<int> GetAdminsCountByTypeAsync(Guid adminTypeId, CancellationToken cancellationToken = default);
}

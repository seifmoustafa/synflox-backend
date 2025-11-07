using Domain.Entities.Authentication;

namespace Domain.Interfaces;

public interface IAdminRepository : IBaseRepository<Guid, Admin>
{
    Task<Admin> GetByUserNameAsync(string username);
}

using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class AdminTypeRepository : BaseRepository<Guid, AdminType>, IAdminTypeRepository
{
    public AdminTypeRepository(ApplicationDBContext context) : base(context)
    {
    }
}


using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class ModuleRepository : BaseRepository<Guid, Module>, IModuleRepository
{
    public ModuleRepository(ApplicationDBContext context) : base(context)
    {
    }
}


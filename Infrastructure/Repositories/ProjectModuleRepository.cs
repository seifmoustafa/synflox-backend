using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class ProjectModuleRepository : BaseRepository<Guid, ProjectModule>, IProjectModuleRepository
{
    public ProjectModuleRepository(ApplicationDBContext context) : base(context)
    {
    }
}


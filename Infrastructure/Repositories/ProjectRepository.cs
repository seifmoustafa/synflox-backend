using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class ProjectRepository : BaseRepository<Guid, Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDBContext context) : base(context)
    {
    }
}


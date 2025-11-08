using Domain.Entities.Licensing;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for Project entity operations.
/// </summary>
public interface IProjectRepository : IBaseRepository<Guid, Project>
{
}


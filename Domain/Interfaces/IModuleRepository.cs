using Domain.Entities.Licensing;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for Module entity operations.
/// </summary>
public interface IModuleRepository : IBaseRepository<Guid, Module>
{
}


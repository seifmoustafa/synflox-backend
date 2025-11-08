using Domain.Entities.Authentication;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for PasswordHistory entity operations.
/// </summary>
public interface IPasswordHistoryRepository : IBaseRepository<Guid, PasswordHistory>
{
}


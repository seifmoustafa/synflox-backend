using Domain.Entities.Settings;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for PasswordPolicy entity operations.
/// </summary>
public interface IPasswordPolicyRepository : IBaseRepository<Guid, PasswordPolicy>
{
}


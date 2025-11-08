using Domain.Entities.Settings;
using Domain.Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class PasswordPolicyRepository : BaseRepository<Guid, PasswordPolicy>, IPasswordPolicyRepository
{
    public PasswordPolicyRepository(ApplicationDBContext context) : base(context)
    {
    }
}


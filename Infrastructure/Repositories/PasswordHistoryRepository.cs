using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class PasswordHistoryRepository : BaseRepository<Guid, PasswordHistory>, IPasswordHistoryRepository
{
    public PasswordHistoryRepository(ApplicationDBContext context) : base(context)
    {
    }
}


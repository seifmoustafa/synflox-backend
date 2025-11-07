using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AdminRepository : BaseRepository<Guid, Admin>, IAdminRepository
{
    public AdminRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<Admin> GetByUserNameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Username == username);
    }
}

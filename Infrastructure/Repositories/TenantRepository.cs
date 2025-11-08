using System;
using System.Threading.Tasks;
using Domain.Entities.Tenancy;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TenantRepository : BaseRepository<Guid, Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Name == name && !t.IsDeleted);
    }
}




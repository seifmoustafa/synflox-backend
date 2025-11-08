using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApiKeyRepository : BaseRepository<Guid, ApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash)
    {
        return await _dbSet
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && !k.IsDeleted);
    }

    public async Task<IEnumerable<ApiKey>> GetByCompanyIdAsync(Guid companyId, int skip = 0, int take = 10)
    {
        return await _dbSet
            .Where(k => k.CompanyId == companyId && !k.IsDeleted)
            .OrderByDescending(k => k.CreatedTimestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId)
    {
        return await _dbSet
            .CountAsync(k => k.CompanyId == companyId && !k.IsDeleted);
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SubscriptionHistoryRepository : BaseRepository<Guid, SubscriptionHistory>, ISubscriptionHistoryRepository
{
    public SubscriptionHistoryRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SubscriptionHistory>> GetByCompanyIdAsync(Guid companyId, int skip = 0, int take = 10)
    {
        return await _dbSet
            .Where(h => h.CompanyId == companyId && !h.IsDeleted)
            .OrderByDescending(h => h.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<SubscriptionHistory>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? companyId = null,
        int skip = 0,
        int take = 10)
    {
        var query = _dbSet
            .Where(h => h.Timestamp >= fromDate && h.Timestamp <= toDate && !h.IsDeleted);

        if (companyId.HasValue)
        {
            query = query.Where(h => h.CompanyId == companyId.Value);
        }

        return await query
            .OrderByDescending(h => h.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId)
    {
        return await _dbSet
            .CountAsync(h => h.CompanyId == companyId && !h.IsDeleted);
    }

    public async Task<int> CountByDateRangeAsync(DateTime fromDate, DateTime toDate, Guid? companyId = null)
    {
        var query = _dbSet
            .Where(h => h.Timestamp >= fromDate && h.Timestamp <= toDate && !h.IsDeleted);

        if (companyId.HasValue)
        {
            query = query.Where(h => h.CompanyId == companyId.Value);
        }

        return await query.CountAsync();
    }
}


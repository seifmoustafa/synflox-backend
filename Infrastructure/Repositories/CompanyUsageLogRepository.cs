using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Analytics;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyUsageLogRepository : BaseRepository<Guid, CompanyUsageLog>, ICompanyUsageLogRepository
{
    public CompanyUsageLogRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CompanyUsageLog>> GetByCompanyIdAsync(
        Guid companyId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbSet
            .Where(log => log.CompanyId == companyId && !log.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.RequestTimestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.RequestTimestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(log => log.RequestTimestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<CompanyUsageLog>> GetAllAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 1000)
    {
        var query = _dbSet.Where(log => !log.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.RequestTimestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.RequestTimestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(log => log.RequestTimestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _dbSet
            .Where(log => log.CompanyId == companyId && !log.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.RequestTimestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.RequestTimestamp <= toDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> CountAllAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _dbSet.Where(log => !log.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.RequestTimestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.RequestTimestamp <= toDate.Value);
        }

        return await query.CountAsync();
    }
}


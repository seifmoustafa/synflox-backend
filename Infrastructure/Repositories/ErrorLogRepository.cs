using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Logging;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ErrorLogRepository : BaseRepository<Guid, ErrorLog>, IErrorLogRepository
{
    public ErrorLogRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ErrorLog>> GetByDateRangeAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100)
    {
        var query = _dbSet.Where(e => !e.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<ErrorLog?> GetByErrorIdAsync(string errorId)
    {
        return await _dbSet
            .Where(e => e.ErrorId == errorId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ErrorLog>> GetBySeverityAsync(
        string severity,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbSet
            .Where(e => e.Severity == severity && !e.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<int> DeleteOldErrorsAsync(DateTime beforeDate)
    {
        var errors = await _dbSet
            .Where(e => e.Timestamp < beforeDate && !e.IsDeleted)
            .ToListAsync();

        foreach (var error in errors)
        {
            error.IsDeleted = true;
        }

        return errors.Count;
    }
}




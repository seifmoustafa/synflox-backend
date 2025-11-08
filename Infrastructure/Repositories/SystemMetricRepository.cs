using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Metrics;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SystemMetricRepository : BaseRepository<Guid, SystemMetric>, ISystemMetricRepository
{
    public SystemMetricRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SystemMetric>> GetByTypeAsync(
        MetricType metricType,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbSet
            .Where(m => m.MetricType == metricType && !m.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.Timestamp <= toDate.Value);
        }

        return await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<SystemMetric?> GetLatestByTypeAsync(MetricType metricType)
    {
        return await _dbSet
            .Where(m => m.MetricType == metricType && !m.IsDeleted)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<SystemMetric>> GetAggregatedAsync(
        MetricType metricType,
        string aggregationPeriod,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbSet
            .Where(m => m.MetricType == metricType 
                   && m.AggregationPeriod == aggregationPeriod 
                   && !m.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.Timestamp <= toDate.Value);
        }

        return await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<int> DeleteOldMetricsAsync(DateTime beforeDate)
    {
        var metrics = await _dbSet
            .Where(m => m.Timestamp < beforeDate && !m.IsDeleted)
            .ToListAsync();

        foreach (var metric in metrics)
        {
            metric.IsDeleted = true;
        }

        return metrics.Count;
    }
}




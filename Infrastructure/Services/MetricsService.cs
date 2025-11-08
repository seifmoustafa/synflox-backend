using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Metrics;
using Application.Services;
using Domain.Entities.Metrics;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class MetricsService : IMetricsService
{
    private readonly ISystemMetricRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;
    private readonly ILogger<MetricsService> _logger;
    private const string CacheKeyPrefix = "metrics:realtime:";

    public MetricsService(
        ISystemMetricRepository repository,
        IUnitOfWork unitOfWork,
        IDistributedCache cache,
        ILogger<MetricsService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task RecordMetricAsync(MetricType metricType, double value, string? tags = null)
    {
        try
        {
            // Store in cache for real-time access
            var cacheKey = $"{CacheKeyPrefix}{metricType}";
            await _cache.SetStringAsync(cacheKey, value.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            // Store in database (async, non-blocking)
            var metric = new SystemMetric
            {
                Id = Guid.NewGuid(),
                MetricType = metricType,
                Value = value,
                Timestamp = DateTime.UtcNow,
                Tags = tags,
                IsActive = true,
                IsDeleted = false
            };

            await _repository.AddAsync(metric);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric {MetricType}", metricType);
            // Don't throw - metrics should not break the application
        }
    }

    public async Task<MetricSummaryDto> GetSummaryAsync()
    {
        var summary = new MetricSummaryDto();

        // Get latest values from cache or database
        var metricTypes = Enum.GetValues<MetricType>();
        
        foreach (var metricType in metricTypes)
        {
            try
            {
                var cacheKey = $"{CacheKeyPrefix}{metricType}";
                var cachedValue = await _cache.GetStringAsync(cacheKey);
                
                if (double.TryParse(cachedValue, out var value))
                {
                    summary.CurrentMetrics[metricType.ToString()] = value;
                }
                else
                {
                    // Fallback to database
                    var latest = await _repository.GetLatestByTypeAsync(metricType);
                    if (latest != null)
                    {
                        summary.CurrentMetrics[metricType.ToString()] = latest.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting metric {MetricType}", metricType);
            }
        }

        return summary;
    }

    public async Task<MetricHistoryDto> GetHistoryAsync(
        MetricType metricType,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? aggregationPeriod = null)
    {
        IEnumerable<SystemMetric> metrics;

        if (!string.IsNullOrWhiteSpace(aggregationPeriod))
        {
            metrics = await _repository.GetAggregatedAsync(metricType, aggregationPeriod, fromDate, toDate);
        }
        else
        {
            metrics = await _repository.GetByTypeAsync(metricType, fromDate, toDate);
        }

        var history = new MetricHistoryDto
        {
            MetricType = metricType,
            MetricTypeName = metricType.ToString(),
            DataPoints = metrics.Select(m => new MetricDataPoint
            {
                Timestamp = m.Timestamp,
                Value = m.Value,
                Tags = m.Tags
            }).ToList()
        };

        return history;
    }

    public async Task AggregateMetricsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var metricTypes = Enum.GetValues<MetricType>();

            // Aggregate hourly (last hour)
            var oneHourAgo = now.AddHours(-1);
            await AggregateMetricsForPeriodAsync(metricTypes, oneHourAgo, now, "hourly");

            // Aggregate daily (last day)
            var oneDayAgo = now.AddDays(-1);
            await AggregateMetricsForPeriodAsync(metricTypes, oneDayAgo, now, "daily");

            // Aggregate monthly (last month)
            var oneMonthAgo = now.AddMonths(-1);
            await AggregateMetricsForPeriodAsync(metricTypes, oneMonthAgo, now, "monthly");

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating metrics");
        }
    }

    private async Task AggregateMetricsForPeriodAsync(
        MetricType[] metricTypes,
        DateTime fromDate,
        DateTime toDate,
        string period)
    {
        foreach (var metricType in metricTypes)
        {
            try
            {
                var rawMetrics = await _repository.GetByTypeAsync(metricType, fromDate, toDate);
                var metricsList = rawMetrics.ToList();

                if (!metricsList.Any())
                    continue;

                // Calculate average for the period
                var average = metricsList.Average(m => m.Value);

                // Check if aggregated metric already exists for this period
                var existing = await _repository.GetAggregatedAsync(metricType, period, fromDate, toDate);
                var existingList = existing.ToList();

                // Only create if doesn't exist
                if (!existingList.Any(e => e.Timestamp.Date == toDate.Date))
                {
                    var aggregated = new SystemMetric
                    {
                        Id = Guid.NewGuid(),
                        MetricType = metricType,
                        Value = average,
                        Timestamp = toDate,
                        AggregationPeriod = period,
                        IsActive = true,
                        IsDeleted = false
                    };

                    await _repository.AddAsync(aggregated);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error aggregating {MetricType} for {Period}", metricType, period);
            }
        }
    }

    public async Task CleanupOldMetricsAsync(int keepDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-keepDays);
            var deletedCount = await _repository.DeleteOldMetricsAsync(cutoffDate);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} old metrics older than {Date}", deletedCount, cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old metrics");
        }
    }
}




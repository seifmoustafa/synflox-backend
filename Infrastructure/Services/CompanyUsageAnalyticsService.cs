using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Analytics;
using Application.Services;
using Domain.Entities.Analytics;
using Domain.Entities.Common;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class CompanyUsageAnalyticsService : ICompanyUsageAnalyticsService
{
    private readonly ICompanyUsageLogRepository _repository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdEncryptionService _idEncryption;

    public CompanyUsageAnalyticsService(
        ICompanyUsageLogRepository repository,
        ICompanyRepository companyRepository,
        IUnitOfWork unitOfWork,
        IIdEncryptionService idEncryption)
    {
        _repository = repository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _idEncryption = idEncryption;
    }

    public async Task LogUsageAsync(
        Guid companyId,
        string endpoint,
        string method,
        long responseTimeMs,
        int statusCode,
        string? ipAddress = null,
        string? userAgent = null,
        Guid? apiKeyId = null)
    {
        var log = new CompanyUsageLog
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Endpoint = endpoint,
            Method = method,
            RequestTimestamp = DateTime.UtcNow,
            ResponseTimeMs = responseTimeMs,
            StatusCode = statusCode,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ApiKeyId = apiKeyId,
            IsActive = true,
            IsDeleted = false
        };

        await _repository.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CompanyUsageAnalyticsDto> GetCompanyUsageAsync(
        Guid companyId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var logs = await _repository.GetByCompanyIdAsync(companyId, fromDate, toDate);
        var logList = logs.ToList();

        var company = await _companyRepository.GetByIdAsync(companyId, null);

        var analytics = new CompanyUsageAnalyticsDto
        {
            // Encrypt CompanyId manually (special case - not using AutoMapper for analytics DTO)
            CompanyId = _idEncryption.Encrypt(companyId),
            CompanyName = company?.Name ?? "Unknown",
            TotalRequests = logList.Count,
            LastRequestTime = logList.FirstOrDefault()?.RequestTimestamp
        };

        if (logList.Count > 0)
        {
            analytics.AverageResponseTimeMs = logList.Average(l => l.ResponseTimeMs);

            // Group by endpoint
            analytics.RequestsByEndpoint = logList
                .GroupBy(l => l.Endpoint)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by method
            analytics.RequestsByMethod = logList
                .GroupBy(l => l.Method)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by status code
            analytics.RequestsByStatusCode = logList
                .GroupBy(l => l.StatusCode)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by date (daily)
            analytics.RequestsByDate = logList
                .GroupBy(l => l.RequestTimestamp.Date)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        return analytics;
    }

    public async Task<CompanyUsageAnalyticsDto> GetOverallUsageAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var logs = await _repository.GetAllAsync(fromDate, toDate, 0, 10000);
        var logList = logs.ToList();

        var analytics = new CompanyUsageAnalyticsDto
        {
            // Encrypt CompanyId manually (special case - not using AutoMapper for analytics DTO)
            // Guid.Empty encrypted is still Guid.Empty (no change needed, but for consistency)
            CompanyId = Guid.Empty,
            CompanyName = "All Companies",
            TotalRequests = logList.Count,
            LastRequestTime = logList.FirstOrDefault()?.RequestTimestamp
        };

        if (logList.Count > 0)
        {
            analytics.AverageResponseTimeMs = logList.Average(l => l.ResponseTimeMs);

            // Group by endpoint
            analytics.RequestsByEndpoint = logList
                .GroupBy(l => l.Endpoint)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by method
            analytics.RequestsByMethod = logList
                .GroupBy(l => l.Method)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by status code
            analytics.RequestsByStatusCode = logList
                .GroupBy(l => l.StatusCode)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by date (daily)
            analytics.RequestsByDate = logList
                .GroupBy(l => l.RequestTimestamp.Date)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        return analytics;
    }

    public async Task<Dictionary<string, int>> GetUsageByEndpointAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var logs = await _repository.GetAllAsync(fromDate, toDate, 0, 10000);
        var logList = logs.ToList();

        return logList
            .GroupBy(l => l.Endpoint)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<(IEnumerable<CompanyUsageAnalyticsDto> Analytics, PaginationMetadata Meta)> GetUsageByCompanyAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 10)
    {
        // Get all unique company IDs from logs
        var logs = await _repository.GetAllAsync(fromDate, toDate, 0, 100000);
        var companyIds = logs.Select(l => l.CompanyId).Distinct().ToList();

        var skip = (page - 1) * pageSize;
        var pagedCompanyIds = companyIds.Skip(skip).Take(pageSize).ToList();

        var analyticsList = new List<CompanyUsageAnalyticsDto>();
        foreach (var companyId in pagedCompanyIds)
        {
            var analytics = await GetCompanyUsageAsync(companyId, fromDate, toDate);
            analyticsList.Add(analytics);
        }

        var meta = new PaginationMetadata(companyIds.Count, pageSize, page);
        return (analyticsList, meta);
    }
}


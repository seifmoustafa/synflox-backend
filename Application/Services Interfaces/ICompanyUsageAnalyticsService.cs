using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Analytics;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for company usage analytics.
/// </summary>
public interface ICompanyUsageAnalyticsService
{
    /// <summary>
    /// Logs an API usage event.
    /// </summary>
    Task LogUsageAsync(
        Guid companyId,
        string endpoint,
        string method,
        long responseTimeMs,
        int statusCode,
        string? ipAddress = null,
        string? userAgent = null,
        Guid? apiKeyId = null);

    /// <summary>
    /// Gets usage analytics for a specific company.
    /// </summary>
    Task<CompanyUsageAnalyticsDto> GetCompanyUsageAsync(
        Guid companyId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets overall API usage analytics.
    /// </summary>
    Task<CompanyUsageAnalyticsDto> GetOverallUsageAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets usage analytics by endpoint.
    /// </summary>
    Task<Dictionary<string, int>> GetUsageByEndpointAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets usage analytics by company.
    /// </summary>
    Task<(IEnumerable<CompanyUsageAnalyticsDto> Analytics, PaginationMetadata Meta)> GetUsageByCompanyAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 10);
}


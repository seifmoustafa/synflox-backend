using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Analytics;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for CompanyUsageLog entity operations.
/// </summary>
public interface ICompanyUsageLogRepository : IBaseRepository<Guid, CompanyUsageLog>
{
    /// <summary>
    /// Gets usage logs for a company within a date range.
    /// </summary>
    Task<IEnumerable<CompanyUsageLog>> GetByCompanyIdAsync(
        Guid companyId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets all usage logs within a date range.
    /// </summary>
    Task<IEnumerable<CompanyUsageLog>> GetAllAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 1000);

    /// <summary>
    /// Counts requests for a company within a date range.
    /// </summary>
    Task<int> CountByCompanyIdAsync(Guid companyId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Counts all requests within a date range.
    /// </summary>
    Task<int> CountAllAsync(DateTime? fromDate = null, DateTime? toDate = null);
}


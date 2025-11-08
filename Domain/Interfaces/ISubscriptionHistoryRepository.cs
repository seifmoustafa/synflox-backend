using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for SubscriptionHistory entity operations.
/// </summary>
public interface ISubscriptionHistoryRepository : IBaseRepository<Guid, SubscriptionHistory>
{
    /// <summary>
    /// Gets all history records for a specific company.
    /// </summary>
    Task<IEnumerable<SubscriptionHistory>> GetByCompanyIdAsync(Guid companyId, int skip = 0, int take = 10);

    /// <summary>
    /// Gets history records within a date range.
    /// </summary>
    Task<IEnumerable<SubscriptionHistory>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? companyId = null,
        int skip = 0,
        int take = 10);

    /// <summary>
    /// Counts history records for a company.
    /// </summary>
    Task<int> CountByCompanyIdAsync(Guid companyId);

    /// <summary>
    /// Counts history records within a date range.
    /// </summary>
    Task<int> CountByDateRangeAsync(DateTime fromDate, DateTime toDate, Guid? companyId = null);
}


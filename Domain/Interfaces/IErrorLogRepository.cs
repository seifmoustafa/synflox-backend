using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Logging;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for ErrorLog entities.
/// </summary>
public interface IErrorLogRepository : IBaseRepository<Guid, ErrorLog>
{
    /// <summary>
    /// Gets errors within a date range.
    /// </summary>
    Task<IEnumerable<ErrorLog>> GetByDateRangeAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100);

    /// <summary>
    /// Gets error by ErrorId.
    /// </summary>
    Task<ErrorLog?> GetByErrorIdAsync(string errorId);

    /// <summary>
    /// Gets errors by severity.
    /// </summary>
    Task<IEnumerable<ErrorLog>> GetBySeverityAsync(
        string severity,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Deletes errors older than the specified date.
    /// </summary>
    Task<int> DeleteOldErrorsAsync(DateTime beforeDate);
}




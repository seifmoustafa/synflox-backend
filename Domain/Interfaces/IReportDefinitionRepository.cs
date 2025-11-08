using Domain.Entities.Reporting;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for ReportDefinition entity operations.
/// </summary>
public interface IReportDefinitionRepository : IBaseRepository<Guid, ReportDefinition>
{
    /// <summary>
    /// Gets all active reports.
    /// </summary>
    Task<IEnumerable<ReportDefinition>> GetActiveReportsAsync();

    /// <summary>
    /// Gets all pre-built reports.
    /// </summary>
    Task<IEnumerable<ReportDefinition>> GetPreBuiltReportsAsync();

    /// <summary>
    /// Gets a report by report type.
    /// </summary>
    Task<ReportDefinition?> GetByReportTypeAsync(string reportType);
}


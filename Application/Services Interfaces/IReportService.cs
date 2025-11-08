using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Reporting;

namespace Application.Services;

/// <summary>
/// Service interface for generating reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Gets all available reports (pre-built and custom).
    /// </summary>
    Task<IEnumerable<ReportDefinitionDto>> GetAvailableReportsAsync();

    /// <summary>
    /// Generates a report by report ID.
    /// </summary>
    Task<ReportResultDto> GenerateReportAsync(
        Guid reportId,
        Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Generates a pre-built report by type.
    /// </summary>
    Task<ReportResultDto> GeneratePreBuiltReportAsync(
        string reportType,
        Dictionary<string, object>? parameters = null);
}




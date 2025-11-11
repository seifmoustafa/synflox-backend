using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Company;
using Application.DTOs.SubscriptionHistory;

namespace Application.Services;

/// <summary>
/// Service interface for exporting data to various formats.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports companies to CSV format.
    /// </summary>
    Task<byte[]> ExportCompaniesToCsvAsync(IEnumerable<CompanyDto> companies);

    /// <summary>
    /// Exports companies to Excel format.
    /// </summary>
    Task<byte[]> ExportCompaniesToExcelAsync(IEnumerable<CompanyDto> companies);

    /// <summary>
    /// Exports subscription history to CSV format.
    /// </summary>
    Task<byte[]> ExportSubscriptionHistoryToCsvAsync(IEnumerable<SubscriptionHistoryDto> history);

    /// <summary>
    /// Exports subscription history to Excel format.
    /// </summary>
    Task<byte[]> ExportSubscriptionHistoryToExcelAsync(IEnumerable<SubscriptionHistoryDto> history);
}




using System;
using System.Threading.Tasks;
using Application.DTOs.Security;

namespace Application.Services
{
    /// <summary>
    /// Service for generating and exporting security reports
    /// </summary>
    public interface ISecurityReportService
    {
        /// <summary>
        /// Generate security report data
        /// </summary>
        Task<SecurityReportDataDto> GenerateReportDataAsync(Guid adminId, SecurityReportRequest request);

        /// <summary>
        /// Export security report as file (PDF/Excel/JSON)
        /// </summary>
        Task<SecurityReportExportDto> ExportSecurityReportAsync(Guid adminId, SecurityReportRequest request);
    }
}

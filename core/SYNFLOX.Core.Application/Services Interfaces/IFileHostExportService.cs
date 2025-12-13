using System;
using System.Threading.Tasks;
using Application.DTOs.Security;

namespace Application.Services
{
    /// <summary>
    /// Service for exporting files to FileHost with auto-cleanup
    /// Files are automatically deleted after 30 minutes
    /// </summary>
    public interface IFileHostExportService
    {
        /// <summary>
        /// Save file to FileHost and return download URL
        /// File will be auto-deleted after 30 minutes
        /// </summary>
        Task<FileHostExportResponse> SaveExportFileAsync(
            byte[] fileContent,
            string fileName,
            string contentType,
            string exportType);

        /// <summary>
        /// Get file path for specific export type
        /// Uses appsettings.json FileHost configuration
        /// </summary>
        string GetStoragePath(string exportType);

        /// <summary>
        /// Clean up expired files (called by background job)
        /// </summary>
        Task CleanupExpiredFilesAsync();
    }
}

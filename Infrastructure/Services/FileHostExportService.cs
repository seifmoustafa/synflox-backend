using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Security;
using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// FileHost-based export service with 30-minute auto-cleanup
    /// </summary>
    public class FileHostExportService : IFileHostExportService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<FileHostExportService> _logger;
        private const int EXPIRY_MINUTES = 30;

        public FileHostExportService(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<FileHostExportService> logger)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<FileHostExportResponse> SaveExportFileAsync(
            byte[] fileContent,
            string fileName,
            string contentType,
            string exportType)
        {
            try
            {
                // Get storage path based on export type
                var storagePath = GetStoragePath(exportType);
                var requestPath = GetRequestPath(exportType);

                // Ensure directory exists with full path
                var fullStoragePath = Path.Combine(Directory.GetCurrentDirectory(), storagePath);
                if (!Directory.Exists(fullStoragePath))
                {
                    Directory.CreateDirectory(fullStoragePath);
                }

                // Use actual filename (sanitized) + timestamp to avoid conflicts
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                var uniqueFileName = $"{baseName}_{timestamp}{extension}";
                var fullPath = Path.Combine(fullStoragePath, uniqueFileName);

                // Save file as binary (works for PDF, Excel, Text, JSON)
                await File.WriteAllBytesAsync(fullPath, fileContent);
                
                _logger.LogInformation(
                    "Export file saved: {FileName} ({Size} bytes) at {Path}",
                    uniqueFileName, fileContent.Length, fullPath);

                // Build download URL
                var request = _httpContextAccessor.HttpContext?.Request;
                var baseUrl = request != null 
                    ? $"{request.Scheme}://{request.Host}" 
                    : _configuration["BaseUrl"] ?? "http://localhost:5000";

                var downloadUrl = $"{baseUrl}{requestPath}/{uniqueFileName}";

                var now = DateTime.UtcNow;
                var expiresAt = now.AddMinutes(EXPIRY_MINUTES);

                return new FileHostExportResponse
                {
                    FileId = Path.GetFileNameWithoutExtension(uniqueFileName),
                    DownloadUrl = downloadUrl,
                    FileName = uniqueFileName,
                    ContentType = contentType,
                    FileSizeBytes = fileContent.Length,
                    GeneratedAt = now,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving export file: {FileName}", fileName);
                throw;
            }
        }

        public string GetStoragePath(string exportType)
        {
            return exportType.ToLower() switch
            {
                "pdf" => _configuration["PdfSettings:StoragePath"] ?? "FileHost/Pdfs",
                "excel" => _configuration["AllFileSettings:StoragePath"] ?? "FileHost/Files",
                "json" => _configuration["AllFileSettings:StoragePath"] ?? "FileHost/Files",
                "text" => _configuration["AllFileSettings:StoragePath"] ?? "FileHost/Files",
                _ => _configuration["AllFileSettings:StoragePath"] ?? "FileHost/Files"
            };
        }

        private string GetRequestPath(string exportType)
        {
            return exportType.ToLower() switch
            {
                "pdf" => _configuration["PdfSettings:RequestPath"] ?? "/pdfs",
                "excel" => _configuration["AllFileSettings:RequestPath"] ?? "/files",
                "json" => _configuration["AllFileSettings:RequestPath"] ?? "/files",
                "text" => _configuration["AllFileSettings:RequestPath"] ?? "/files",
                _ => _configuration["AllFileSettings:RequestPath"] ?? "/files"
            };
        }

        public async Task CleanupExpiredFilesAsync()
        {
            try
            {
                var exportTypes = new[] { "pdf", "excel", "json", "text" };
                var now = DateTime.UtcNow;
                var deletedCount = 0;

                foreach (var exportType in exportTypes)
                {
                    var storagePath = GetStoragePath(exportType);

                    if (!Directory.Exists(storagePath))
                        continue;

                    var files = Directory.GetFiles(storagePath);

                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        var age = now - fileInfo.CreationTimeUtc;

                        // Delete files older than 30 minutes
                        if (age.TotalMinutes > EXPIRY_MINUTES)
                        {
                            try
                            {
                                File.Delete(file);
                                deletedCount++;
                                _logger.LogInformation("Deleted expired export file: {FileName}", fileInfo.Name);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete expired file: {FileName}", fileInfo.Name);
                            }
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired export files", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during export files cleanup");
            }
        }
    }
}

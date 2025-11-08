using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Logging;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for error logging and tracking.
/// </summary>
public interface IErrorLogService
{
    /// <summary>
    /// Logs an error with full context.
    /// </summary>
    Task<string> LogErrorAsync(
        Exception exception,
        string? httpMethod = null,
        string? requestPath = null,
        string? queryString = null,
        string? requestBody = null,
        int? statusCode = null,
        string? ipAddress = null,
        string? userAgent = null,
        Guid? userId = null,
        Guid? companyId = null,
        string? contextData = null,
        string severity = "Error");

    /// <summary>
    /// Gets errors within a date range.
    /// </summary>
    Task<(IEnumerable<ErrorLogDto> Errors, PaginationMetadata Meta)> GetErrorsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? severity = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Gets error by ErrorId.
    /// </summary>
    Task<ErrorLogDto?> GetErrorByIdAsync(string errorId);

    /// <summary>
    /// Cleans up old errors (keeps only last N days).
    /// </summary>
    Task<int> CleanupOldErrorsAsync(int keepDays = 90);
}




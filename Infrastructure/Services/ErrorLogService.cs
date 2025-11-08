using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Logging;
using Application.Services;
using AutoMapper;
using Domain.Entities.Logging;
using Domain.Entities.Common;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ErrorLogService : IErrorLogService
{
    private readonly IErrorLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(
        IErrorLogRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ErrorLogService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<string> LogErrorAsync(
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
        string severity = "Error")
    {
        try
        {
            var errorId = Guid.NewGuid().ToString("N").Substring(0, 16); // Short unique ID

            // Truncate request body if too long
            if (!string.IsNullOrEmpty(requestBody) && requestBody.Length > 5000)
            {
                requestBody = requestBody.Substring(0, 5000) + "... (truncated)";
            }

            var errorLog = new ErrorLog
            {
                Id = Guid.NewGuid(),
                ErrorId = errorId,
                Message = exception.Message.Length > 2000 
                    ? exception.Message.Substring(0, 2000) 
                    : exception.Message,
                StackTrace = exception.StackTrace,
                ExceptionType = exception.GetType().FullName,
                HttpMethod = httpMethod,
                RequestPath = requestPath,
                QueryString = queryString,
                RequestBody = requestBody,
                StatusCode = statusCode,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                UserId = userId,
                CompanyId = companyId,
                ContextData = contextData,
                Timestamp = DateTime.UtcNow,
                Severity = severity,
                IsActive = true,
                IsDeleted = false
            };

            await _repository.AddAsync(errorLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogError(exception, "Error logged with ID: {ErrorId}", errorId);

            return errorId;
        }
        catch (Exception ex)
        {
            // Don't let error logging break the application
            _logger.LogCritical(ex, "Failed to log error");
            return "LOG_FAILED";
        }
    }

    public async Task<(IEnumerable<ErrorLogDto> Errors, PaginationMetadata Meta)> GetErrorsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? severity = null,
        int page = 1,
        int pageSize = 50)
    {
        IEnumerable<ErrorLog> errors;

        if (!string.IsNullOrWhiteSpace(severity))
        {
            errors = await _repository.GetBySeverityAsync(severity, fromDate, toDate);
        }
        else
        {
            var skip = (page - 1) * pageSize;
            errors = await _repository.GetByDateRangeAsync(fromDate, toDate, skip, pageSize);
        }

        var errorsList = errors.ToList();
        var totalCount = errorsList.Count;

        // If we filtered by severity, we need to get total count separately
        if (!string.IsNullOrWhiteSpace(severity))
        {
            var allErrors = await _repository.GetBySeverityAsync(severity, fromDate, toDate);
            totalCount = allErrors.Count();
            
            // Apply pagination
            errorsList = errorsList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        var dtos = _mapper.Map<IEnumerable<ErrorLogDto>>(errorsList);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    public async Task<ErrorLogDto?> GetErrorByIdAsync(string errorId)
    {
        var error = await _repository.GetByErrorIdAsync(errorId);
        return error != null ? _mapper.Map<ErrorLogDto>(error) : null;
    }

    public async Task<int> CleanupOldErrorsAsync(int keepDays = 90)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-keepDays);
            var deletedCount = await _repository.DeleteOldErrorsAsync(cutoffDate);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old errors older than {Date}", deletedCount, cutoffDate);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old errors");
            return 0;
        }
    }
}


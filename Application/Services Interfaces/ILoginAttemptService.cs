using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing login attempts.
/// </summary>
public interface ILoginAttemptService
{
    /// <summary>
    /// Records a login attempt.
    /// </summary>
    Task RecordAttemptAsync(
        string username,
        bool success,
        string? ipAddress = null,
        string? failureReason = null,
        Guid? adminId = null);

    /// <summary>
    /// Gets recent failed login attempts for a username.
    /// </summary>
    Task<IEnumerable<LoginAttemptDto>> GetRecentFailedAttemptsAsync(
        string username,
        int minutes = 30);

    /// <summary>
    /// Checks if an account is locked due to too many failed attempts.
    /// </summary>
    Task<bool> IsAccountLockedAsync(string username, string? ipAddress = null);

    /// <summary>
    /// Gets all login attempts with optional filters.
    /// </summary>
    Task<(IEnumerable<LoginAttemptDto> Attempts, PaginationMetadata Meta)> GetAllAttemptsAsync(
        string? username = null,
        bool? success = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 10);
}


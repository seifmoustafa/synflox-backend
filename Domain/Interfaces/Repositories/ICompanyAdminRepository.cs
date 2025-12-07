using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for CompanyAdmin entity.
/// Provides operations for managing company administrator accounts.
/// </summary>
public interface ICompanyAdminRepository : IBaseRepository<Guid, CompanyAdmin>
{
    /// <summary>
    /// Get admin by company ID.
    /// </summary>
    Task<CompanyAdmin?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get admin by username.
    /// </summary>
    Task<CompanyAdmin?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get admin by email.
    /// </summary>
    Task<CompanyAdmin?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if username exists.
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update last login information.
    /// </summary>
    Task UpdateLastLoginAsync(Guid adminId, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update current session information.
    /// </summary>
    Task UpdateCurrentSessionAsync(Guid adminId, string? sessionId, string? deviceHash, string? deviceName, string? ip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear current session.
    /// </summary>
    Task ClearCurrentSessionAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment failed login attempts and lock if needed.
    /// </summary>
    Task IncrementFailedLoginAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset failed login attempts.
    /// </summary>
    Task ResetFailedLoginAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update password hash.
    /// </summary>
    Task UpdatePasswordAsync(Guid adminId, string passwordHash, string salt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update last activity timestamp.
    /// </summary>
    Task UpdateLastActivityAsync(Guid adminId, CancellationToken cancellationToken = default);
}

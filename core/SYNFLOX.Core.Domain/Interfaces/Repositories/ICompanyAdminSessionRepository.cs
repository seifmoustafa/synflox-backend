using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for CompanyAdminSession entity.
/// Provides operations for managing admin login sessions.
/// </summary>
public interface ICompanyAdminSessionRepository : IBaseRepository<Guid, CompanyAdminSession>
{
    /// <summary>
    /// Get session by session ID string.
    /// </summary>
    Task<CompanyAdminSession?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active sessions for an admin.
    /// </summary>
    Task<List<CompanyAdminSession>> GetActiveSessionsByAdminAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get session history for an admin.
    /// </summary>
    Task<List<CompanyAdminSession>> GetSessionHistoryAsync(Guid adminId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// End all active sessions for an admin.
    /// </summary>
    Task EndAllSessionsAsync(Guid adminId, SessionEndReason reason, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// End a specific session.
    /// </summary>
    Task EndSessionAsync(string sessionId, SessionEndReason reason, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update session activity (last activity time and action count).
    /// </summary>
    Task UpdateSessionActivityAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of active sessions for an admin.
    /// </summary>
    Task<int> GetActiveSessionCountAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired sessions (set IsActive = false).
    /// </summary>
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}

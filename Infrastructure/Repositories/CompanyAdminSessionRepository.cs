using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces.Repositories;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for CompanyAdminSession entity.
/// </summary>
public class CompanyAdminSessionRepository : BaseRepository<Guid, CompanyAdminSession>, ICompanyAdminSessionRepository
{
    public CompanyAdminSessionRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<CompanyAdminSession?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Admin)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && !s.IsDeleted, cancellationToken);
    }

    public async Task<List<CompanyAdminSession>> GetActiveSessionsByAdminAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.AdminId == adminId && s.IsActive && !s.IsDeleted && s.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CompanyAdminSession>> GetSessionHistoryAsync(Guid adminId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.AdminId == adminId && !s.IsDeleted)
            .OrderByDescending(s => s.StartedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task EndAllSessionsAsync(Guid adminId, SessionEndReason reason, string? notes = null, CancellationToken cancellationToken = default)
    {
        var sessions = await _dbSet
            .Where(s => s.AdminId == adminId && s.IsActive && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAtUtc = DateTime.UtcNow;
            session.EndReason = reason;
            session.EndNotes = notes ?? string.Empty;
        }
    }

    public async Task EndSessionAsync(string sessionId, SessionEndReason reason, string? notes = null, CancellationToken cancellationToken = default)
    {
        var session = await _dbSet.FirstOrDefaultAsync(s => s.SessionId == sessionId && !s.IsDeleted, cancellationToken);
        if (session != null)
        {
            session.IsActive = false;
            session.EndedAtUtc = DateTime.UtcNow;
            session.EndReason = reason;
            session.EndNotes = notes ?? string.Empty;
        }
    }

    public async Task UpdateSessionActivityAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _dbSet.FirstOrDefaultAsync(s => s.SessionId == sessionId && !s.IsDeleted, cancellationToken);
        if (session != null)
        {
            session.LastActivityAtUtc = DateTime.UtcNow;
            session.ActionCount++;
        }
    }

    public async Task<int> GetActiveSessionCountAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(s => s.AdminId == adminId && s.IsActive && !s.IsDeleted && s.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);
    }

    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredSessions = await _dbSet
            .Where(s => s.IsActive && s.ExpiresAtUtc <= DateTime.UtcNow && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
            session.EndedAtUtc = DateTime.UtcNow;
            session.EndReason = SessionEndReason.Timeout;
        }
    }
}

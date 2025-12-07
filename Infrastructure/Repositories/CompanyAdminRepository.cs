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
/// Repository implementation for CompanyAdmin entity.
/// </summary>
public class CompanyAdminRepository : BaseRepository<Guid, CompanyAdmin>, ICompanyAdminRepository
{
    public CompanyAdminRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<CompanyAdmin?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && !a.IsDeleted, cancellationToken);
    }

    public async Task<CompanyAdmin?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Username.ToLower() == username.ToLower() && !a.IsDeleted, cancellationToken);
    }

    public async Task<CompanyAdmin?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Email != null && a.Email.ToLower() == email.ToLower() && !a.IsDeleted, cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(a => a.Username.ToLower() == username.ToLower() && !a.IsDeleted);
        
        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task UpdateLastLoginAsync(Guid adminId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var admin = await _dbSet.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin != null)
        {
            admin.LastLoginAtUtc = DateTime.UtcNow;
            admin.LastLoginIp = ipAddress ?? "Unknown";
            admin.TotalLogins++;
            admin.FailedLoginAttempts = 0;
            admin.LockedUntilUtc = DateTime.MinValue;
        }
    }

    public async Task UpdateCurrentSessionAsync(Guid adminId, string? sessionId, string? deviceHash, string? deviceName, string? ip, CancellationToken cancellationToken = default)
    {
        var admin = await _dbSet.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin != null)
        {
            admin.CurrentSessionId = sessionId ?? string.Empty;
            admin.CurrentDeviceHash = deviceHash ?? string.Empty;
            admin.CurrentDeviceName = deviceName ?? string.Empty;
            admin.CurrentSessionIp = ip ?? string.Empty;
            admin.SessionStartedAtUtc = !string.IsNullOrEmpty(sessionId) ? DateTime.UtcNow : DateTime.MinValue;
            admin.LastActivityAtUtc = !string.IsNullOrEmpty(sessionId) ? DateTime.UtcNow : DateTime.MinValue;
        }
    }

    public async Task ClearCurrentSessionAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var admin = await _dbSet.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin != null)
        {
            admin.CurrentSessionId = string.Empty;
            admin.CurrentDeviceHash = string.Empty;
            admin.CurrentDeviceName = string.Empty;
            admin.CurrentSessionIp = string.Empty;
            admin.SessionStartedAtUtc = DateTime.MinValue;
        }
    }

    public async Task IncrementFailedLoginAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var admin = await _dbSet.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin != null)
        {
            admin.FailedLoginAttempts++;
            
            if (admin.MaxFailedAttempts > 0 && admin.FailedLoginAttempts >= admin.MaxFailedAttempts)
            {
                admin.LockedUntilUtc = DateTime.UtcNow.AddMinutes(admin.LockoutDurationMinutes);
            }
        }
    }

    public async Task ResetFailedLoginAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var admin = await _dbSet.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin != null)
        {
            admin.FailedLoginAttempts = 0;
            admin.LockedUntilUtc = DateTime.MinValue;
        }
    }

    public async Task UpdatePasswordAsync(Guid adminId, string passwordHash, string salt, CancellationToken cancellationToken = default)
    {
        var admin = await _dbSet.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin != null)
        {
            admin.PasswordHash = passwordHash;
            admin.Salt = salt;
            admin.PasswordChangedAtUtc = DateTime.UtcNow;
            admin.MustChangePassword = false;
        }
    }

    public async Task UpdateLastActivityAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var admin = await _dbSet.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin != null)
        {
            admin.LastActivityAtUtc = DateTime.UtcNow;
        }
    }
}

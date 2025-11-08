using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LoginAttemptRepository : BaseRepository<Guid, LoginAttempt>, ILoginAttemptRepository
{
    public LoginAttemptRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LoginAttempt>> GetRecentFailedAttemptsAsync(
        string username,
        DateTime since,
        int limit = 10)
    {
        return await _dbSet
            .Where(a => a.Username == username &&
                       !a.Success &&
                       a.AttemptedAt >= since &&
                       !a.IsDeleted)
            .OrderByDescending(a => a.AttemptedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<LoginAttempt>> GetRecentFailedAttemptsByIpAsync(
        string ipAddress,
        DateTime since,
        int limit = 10)
    {
        return await _dbSet
            .Where(a => a.IpAddress == ipAddress &&
                       !a.Success &&
                       a.AttemptedAt >= since &&
                       !a.IsDeleted)
            .OrderByDescending(a => a.AttemptedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> CountFailedAttemptsAsync(string username, DateTime since)
    {
        return await _dbSet
            .CountAsync(a => a.Username == username &&
                           !a.Success &&
                           a.AttemptedAt >= since &&
                           !a.IsDeleted);
    }

    public async Task<int> CountFailedAttemptsByIpAsync(string ipAddress, DateTime since)
    {
        return await _dbSet
            .CountAsync(a => a.IpAddress == ipAddress &&
                           !a.Success &&
                           a.AttemptedAt >= since &&
                           !a.IsDeleted);
    }
}


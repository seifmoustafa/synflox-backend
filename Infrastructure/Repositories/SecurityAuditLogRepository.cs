using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SecurityAuditLogRepository : BaseRepository<Guid, SecurityAuditLog>, ISecurityAuditLogRepository
    {
        public SecurityAuditLogRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<List<SecurityAuditLog>> GetByAdminIdAsync(Guid adminId, int pageNumber = 1, int pageSize = 50)
        {
            return await _dbSet
                .Where(log => log.AdminId == adminId)
                .OrderByDescending(log => log.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountRecentFailedAttemptsByIpAsync(string ipAddress, DateTime since, string eventType)
        {
            return await _dbSet
                .Where(log => log.IpAddress == ipAddress 
                           && log.CreatedAt >= since 
                           && log.EventType == eventType 
                           && !log.Success)
                .CountAsync();
        }

        public async Task<int> CountRecentEventsByIpAsync(string ipAddress, DateTime since, string eventType)
        {
            return await _dbSet
                .Where(log => log.IpAddress == ipAddress 
                           && log.CreatedAt >= since 
                           && log.EventType == eventType)
                .CountAsync();
        }

        public async Task<List<SecurityAuditLog>> GetSuspiciousActivityAsync(DateTime since, int failureThreshold = 5)
        {
            return await _dbSet
                .Where(log => log.CreatedAt >= since && !log.Success)
                .GroupBy(log => log.IpAddress)
                .Where(g => g.Count() >= failureThreshold)
                .SelectMany(g => g.OrderByDescending(log => log.CreatedAt))
                .ToListAsync();
        }

        public async Task DeleteOldLogsAsync(DateTime olderThan)
        {
            var oldLogs = await _dbSet
                .Where(log => log.CreatedAt < olderThan)
                .ToListAsync();

            _dbSet.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
        }
    }
}

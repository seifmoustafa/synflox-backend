using Domain.Entities.Activity;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ActivityLogRepository : BaseRepository<Guid, ActivityLog>, IActivityLogRepository
    {
        public ActivityLogRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<List<ActivityLog>> GetRecentAsync(int count = 10)
        {
            return await _dbSet
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetByEntityTypeAsync(string entityType, int count = 50)
        {
            return await _dbSet
                .Where(a => a.EntityType == entityType)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetByAdminAsync(Guid adminId, int count = 50)
        {
            return await _dbSet
                .Where(a => a.PerformedBy == adminId)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetByEntityAsync(Guid entityId, int count = 50)
        {
            return await _dbSet
                .Where(a => a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetCountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .CountAsync(a => a.Timestamp >= startDate && a.Timestamp <= endDate);
        }
    }
}

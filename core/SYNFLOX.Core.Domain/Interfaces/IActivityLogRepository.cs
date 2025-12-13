using Domain.Entities.Activity;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository interface for ActivityLog operations.
    /// </summary>
    public interface IActivityLogRepository : IBaseRepository<Guid, ActivityLog>
    {
        /// <summary>
        /// Gets recent activities with pagination.
        /// </summary>
        Task<List<ActivityLog>> GetRecentAsync(int count = 10);

        /// <summary>
        /// Gets activities by entity type.
        /// </summary>
        Task<List<ActivityLog>> GetByEntityTypeAsync(string entityType, int count = 50);

        /// <summary>
        /// Gets activities by admin who performed them.
        /// </summary>
        Task<List<ActivityLog>> GetByAdminAsync(Guid adminId, int count = 50);

        /// <summary>
        /// Gets activities for a specific entity.
        /// </summary>
        Task<List<ActivityLog>> GetByEntityAsync(Guid entityId, int count = 50);

        /// <summary>
        /// Gets activity count by date range.
        /// </summary>
        Task<int> GetCountByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}

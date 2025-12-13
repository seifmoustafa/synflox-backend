using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository interface for SecurityAuditLog operations
    /// </summary>
    public interface ISecurityAuditLogRepository : IBaseRepository<Guid, SecurityAuditLog>
    {
        /// <summary>
        /// Get audit logs for a specific admin
        /// </summary>
        Task<List<SecurityAuditLog>> GetByAdminIdAsync(Guid adminId, int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Get recent failed login attempts for an IP address
        /// Used for rate limiting and security analysis
        /// </summary>
        Task<int> CountRecentFailedAttemptsByIpAsync(string ipAddress, DateTime since, string eventType);

        /// <summary>
        /// Count events by IP and event type (regardless of success/failure)
        /// Used for rate limiting on operations like code generation
        /// </summary>
        Task<int> CountRecentEventsByIpAsync(string ipAddress, DateTime since, string eventType);

        /// <summary>
        /// Get suspicious activity (multiple failures from same IP)
        /// </summary>
        Task<List<SecurityAuditLog>> GetSuspiciousActivityAsync(DateTime since, int failureThreshold = 5);

        /// <summary>
        /// Clean up old audit logs (optional housekeeping)
        /// </summary>
        Task DeleteOldLogsAsync(DateTime olderThan);

        /// <summary>
        /// Get audit logs by admin ID and event type within a time range
        /// Used for security analytics and statistics
        /// </summary>
        Task<List<SecurityAuditLog>> GetByAdminAndEventTypeAsync(
            Guid adminId,
            string eventType,
            DateTime? startDate,
            DateTime? endDate);

        /// <summary>
        /// Get recent audit logs for an admin (sorted by timestamp descending)
        /// Used for security dashboard recent activity
        /// </summary>
        Task<List<SecurityAuditLog>> GetRecentByAdminAsync(Guid adminId, DateTime since, int limit);
    }
}

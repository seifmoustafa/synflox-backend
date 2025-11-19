using System;
using System.Threading.Tasks;
using Application.DTOs.Security;

namespace Application.Services
{
    /// <summary>
    /// Service for security analytics and dashboard data
    /// Provides comprehensive security overview for admins
    /// </summary>
    public interface ISecurityAnalyticsService
    {
        /// <summary>
        /// Get comprehensive security dashboard data for an admin
        /// Includes 2FA stats, backup codes, recent events, failed logins
        /// Calculates security score based on various factors
        /// </summary>
        /// <param name="adminId">Admin ID to get security data for</param>
        /// <returns>Complete security dashboard data</returns>
        Task<SecurityDashboardDto> GetSecurityDashboardAsync(Guid adminId);
    }
}

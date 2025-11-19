using System;
using System.Threading.Tasks;
using Application.DTOs.Security;

namespace Application.Services
{
    /// <summary>
    /// Service for advanced security analytics with time-series data and trends
    /// </summary>
    public interface IAdvancedSecurityAnalyticsService
    {
        /// <summary>
        /// Get advanced security analytics for an admin
        /// Includes trends, patterns, and predictions
        /// </summary>
        Task<AdvancedSecurityAnalyticsDto> GetAdvancedAnalyticsAsync(Guid adminId, DateTime startDate, DateTime endDate);
    }
}

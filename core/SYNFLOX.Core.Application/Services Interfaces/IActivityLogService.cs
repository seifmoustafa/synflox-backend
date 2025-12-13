using Domain.Enums;

namespace Application.Services_Interfaces
{
    /// <summary>
    /// Service for logging and retrieving system activities
    /// </summary>
    public interface IActivityLogService
    {
        /// <summary>
        /// Logs an activity to the system
        /// </summary>
        Task LogActivityAsync(
            ActivityActionType actionType,
            string entityType,
            Guid entityId,
            string entityName,
            string? description = null,
            Guid? performedBy = null,
            string? performedByName = null,
            string? metadata = null,
            string? ipAddress = null);

        /// <summary>
        /// Logs a company-related activity
        /// </summary>
        Task LogCompanyActivityAsync(
            ActivityActionType actionType,
            Guid companyId,
            string companyName,
            Guid? performedBy = null,
            string? performedByName = null,
            string? description = null);

        /// <summary>
        /// Logs a subscription-related activity
        /// </summary>
        Task LogSubscriptionActivityAsync(
            ActivityActionType actionType,
            Guid subscriptionId,
            string subscriptionName,
            Guid? performedBy = null,
            string? performedByName = null,
            string? description = null);

        /// <summary>
        /// Logs an admin-related activity
        /// </summary>
        Task LogAdminActivityAsync(
            ActivityActionType actionType,
            Guid adminId,
            string adminName,
            Guid? performedBy = null,
            string? performedByName = null,
            string? description = null);

        /// <summary>
        /// Gets recent activities for dashboard
        /// </summary>
        Task<List<ActivityLogDto>> GetRecentActivitiesAsync(int count = 10);

        /// <summary>
        /// Gets activities by entity type
        /// </summary>
        Task<List<ActivityLogDto>> GetActivitiesByEntityTypeAsync(string entityType, int count = 50);
    }

    /// <summary>
    /// DTO for activity log display
    /// </summary>
    public record ActivityLogDto(
        Guid Id,
        string ActionType,
        string EntityType,
        Guid EntityId,
        string EntityName,
        string? Description,
        Guid? PerformedBy,
        string? PerformedByName,
        DateTime Timestamp,
        string TimeAgo);
}

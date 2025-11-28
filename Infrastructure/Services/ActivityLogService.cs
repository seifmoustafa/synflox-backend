using Application.Services;
using Application.Services_Interfaces;
using Domain.Entities.Activity;
using Domain.Enums;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILocalizationService _localizer;

        public ActivityLogService(
            IActivityLogRepository activityLogRepository,
            IAdminRepository adminRepository,
            IUnitOfWork unitOfWork,
            ILocalizationService localizer)
        {
            _activityLogRepository = activityLogRepository;
            _adminRepository = adminRepository;
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        public async Task LogActivityAsync(
            ActivityActionType actionType,
            string entityType,
            Guid entityId,
            string entityName,
            string? description = null,
            Guid? performedBy = null,
            string? performedByName = null,
            string? metadata = null,
            string? ipAddress = null)
        {
            // If we have a performedBy ID but no name, fetch the admin username
            if (performedBy.HasValue && performedBy.Value != Guid.Empty && string.IsNullOrEmpty(performedByName))
            {
                var admin = await _adminRepository.GetByIdAsync(performedBy.Value, null);
                if (admin != null)
                {
                    // Use username for display
                    performedByName = admin.Username;
                }
            }

            var activity = new ActivityLog
            {
                Id = Guid.NewGuid(),
                ActionType = actionType.ToString(),
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                PerformedBy = performedBy,
                PerformedByName = performedByName ?? _localizer["Activity.System"],
                Timestamp = DateTime.Now, // Use server local time
                Metadata = metadata,
                IpAddress = ipAddress
            };

            await _activityLogRepository.AddAsync(activity);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task LogCompanyActivityAsync(
            ActivityActionType actionType,
            Guid companyId,
            string companyName,
            Guid? performedBy = null,
            string? performedByName = null,
            string? description = null)
        {
            await LogActivityAsync(
                actionType,
                "Company",
                companyId,
                companyName,
                description ?? GetLocalizedDescription(actionType, "Company", companyName),
                performedBy,
                performedByName);
        }

        public async Task LogSubscriptionActivityAsync(
            ActivityActionType actionType,
            Guid subscriptionId,
            string subscriptionName,
            Guid? performedBy = null,
            string? performedByName = null,
            string? description = null)
        {
            await LogActivityAsync(
                actionType,
                "Subscription",
                subscriptionId,
                subscriptionName,
                description ?? GetLocalizedDescription(actionType, "Subscription", subscriptionName),
                performedBy,
                performedByName);
        }

        public async Task LogAdminActivityAsync(
            ActivityActionType actionType,
            Guid adminId,
            string adminName,
            Guid? performedBy = null,
            string? performedByName = null,
            string? description = null)
        {
            await LogActivityAsync(
                actionType,
                "Admin",
                adminId,
                adminName,
                description ?? GetLocalizedDescription(actionType, "Admin", adminName),
                performedBy,
                performedByName);
        }

        public async Task<List<ActivityLogDto>> GetRecentActivitiesAsync(int count = 10)
        {
            var activities = await _activityLogRepository.GetRecentAsync(count);
            return activities.Select(MapToDto).ToList();
        }

        public async Task<List<ActivityLogDto>> GetActivitiesByEntityTypeAsync(string entityType, int count = 50)
        {
            var activities = await _activityLogRepository.GetByEntityTypeAsync(entityType, count);
            return activities.Select(MapToDto).ToList();
        }

        private string GetLocalizedDescription(ActivityActionType actionType, string entityType, string entityName)
        {
            // Get localized entity type
            var localizedEntityType = _localizer[$"Activity.EntityType.{entityType}"];
            
            // Get localized action description
            var key = $"Activity.Action.{actionType}";
            var template = _localizer[key];
            
            // Replace placeholders
            return template
                .Replace("{entityType}", localizedEntityType)
                .Replace("{entityName}", entityName);
        }

        private ActivityLogDto MapToDto(ActivityLog activity)
        {
            // Get localized entity type and action for display
            var localizedEntityType = _localizer[$"Activity.EntityType.{activity.EntityType}"];
            var localizedAction = _localizer[$"Activity.Action.{activity.ActionType}"];
            
            var description = localizedAction
                .Replace("{entityType}", localizedEntityType)
                .Replace("{entityName}", activity.EntityName);

            return new ActivityLogDto(
                activity.Id,
                activity.ActionType,
                activity.EntityType,
                activity.EntityId,
                activity.EntityName,
                description,
                activity.PerformedBy,
                activity.PerformedByName,
                activity.Timestamp,
                GetLocalizedTimeAgo(activity.Timestamp));
        }

        private string GetLocalizedTimeAgo(DateTime timestamp)
        {
            var now = DateTime.Now; // Use server local time
            var diff = now - timestamp;

            if (diff.TotalMinutes < 1) return _localizer["Activity.Time.JustNow"];
            if (diff.TotalMinutes < 60) return string.Format(_localizer["Activity.Time.MinutesAgo"], (int)diff.TotalMinutes);
            if (diff.TotalHours < 24) return string.Format(_localizer["Activity.Time.HoursAgo"], (int)diff.TotalHours);
            if (diff.TotalDays < 7) return string.Format(_localizer["Activity.Time.DaysAgo"], (int)diff.TotalDays);
            if (diff.TotalDays < 30) return string.Format(_localizer["Activity.Time.WeeksAgo"], (int)(diff.TotalDays / 7));
            return timestamp.ToString("MMM dd");
        }
    }
}

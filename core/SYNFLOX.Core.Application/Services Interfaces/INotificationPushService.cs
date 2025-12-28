using Application.DTOs.Notification;

namespace Application.Services;

/// <summary>
/// Interface for publishing notification events to connected clients.
/// Implemented by SignalR hub services in WebAPI layer.
/// </summary>
public interface INotificationPushService
{
    /// <summary>
    /// Push a notification to a specific user via SignalR
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="userType">User type (Admin, CompanyAdmin, etc.)</param>
    /// <param name="notification">Notification payload</param>
    Task PushToUserAsync(Guid userId, string userType, NotificationDto notification);

    /// <summary>
    /// Push a notification to multiple users
    /// </summary>
    Task PushToUsersAsync(IEnumerable<Guid> userIds, string userType, NotificationDto notification);
}

/// <summary>
/// Null implementation for when no SignalR hub is configured (Admin API uses separate hub)
/// </summary>
public class NullNotificationPushService : INotificationPushService
{
    public Task PushToUserAsync(Guid userId, string userType, NotificationDto notification)
    {
        // No-op - SignalR not configured
        return Task.CompletedTask;
    }

    public Task PushToUsersAsync(
        IEnumerable<Guid> userIds,
        string userType,
        NotificationDto notification
    )
    {
        return Task.CompletedTask;
    }
}

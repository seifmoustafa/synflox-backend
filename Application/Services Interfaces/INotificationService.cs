using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Notifications;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a new notification and optionally sends email.
    /// </summary>
    Task<NotificationDto> CreateNotificationAsync(
        Guid companyId,
        Domain.Enums.NotificationType type,
        string title,
        string message,
        string? companyEmail = null);

    /// <summary>
    /// Gets notifications for a specific company.
    /// </summary>
    Task<(IEnumerable<NotificationDto> Notifications, PaginationMetadata Meta)> GetNotificationsByCompanyIdAsync(
        Guid companyId,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets all notifications (for admins).
    /// </summary>
    Task<(IEnumerable<NotificationDto> Notifications, PaginationMetadata Meta)> GetAllNotificationsAsync(
        Guid? companyId = null,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId);

    /// <summary>
    /// Marks all notifications for a company as read.
    /// </summary>
    Task MarkAllAsReadAsync(Guid companyId);

    /// <summary>
    /// Gets unread notification count for a company.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid companyId);
}


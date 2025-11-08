using System;
using Domain.Enums;

namespace Application.DTOs.Notifications;

/// <summary>
/// DTO for notification records.
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TypeName { get; set; } = string.Empty;
}


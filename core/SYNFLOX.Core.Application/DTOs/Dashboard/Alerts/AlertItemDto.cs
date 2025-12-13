namespace Application.DTOs.Dashboard.Alerts;

/// <summary>
/// Individual alert item
/// </summary>
public record AlertItemDto(
    Guid Id,
    AlertPriority Priority,
    AlertCategory Category,
    string Title,
    string Message,
    string? Description,
    
    // Related entity
    string? EntityType,
    Guid? EntityId,
    string? EntityName,
    
    // Timing
    DateTime CreatedAt,
    DateTime? DueDate,
    int? DaysRemaining,
    string TimeAgo,
    
    // Actions
    string PrimaryAction,       // "Renew", "Contact", "Review", etc.
    string? PrimaryActionUrl,
    string? SecondaryAction,
    string? SecondaryActionUrl,
    
    // Visual
    string Icon,
    string Color,
    
    // State
    bool IsRead,
    bool IsDismissed,
    DateTime? DismissedAt,
    Guid? DismissedById
);

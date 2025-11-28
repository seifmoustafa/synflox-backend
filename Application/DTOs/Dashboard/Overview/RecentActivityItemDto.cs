namespace Application.DTOs.Dashboard.Overview;

/// <summary>
/// Recent activity item for the activity feed
/// </summary>
public record RecentActivityItemDto(
    Guid Id,
    string ActionType,      // "Created", "Updated", "Deleted", "Activated", etc.
    string EntityType,      // "Company", "Subscription", "Admin", etc.
    string EntityName,
    Guid EntityId,
    string PerformedBy,     // Admin full name
    Guid PerformedById,
    DateTime PerformedAt,
    string? Description,
    string Icon,
    string Color,
    string TimeAgo          // Localized time ago string
);

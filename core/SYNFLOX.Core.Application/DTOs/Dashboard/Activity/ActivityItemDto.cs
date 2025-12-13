namespace Application.DTOs.Dashboard.Activity;

/// <summary>
/// Activity log item
/// </summary>
public record ActivityItemDto(
    Guid Id,
    string ActionType,          // "Create", "Update", "Delete", "Login", "Logout", etc.
    string EntityType,          // "Company", "Subscription", "Admin", "System"
    string EntityName,
    Guid? EntityId,
    string Description,
    
    // Performer info
    Guid AdminId,
    string AdminName,
    string AdminUsername,
    string? AdminProfilePicture,
    
    // Timestamp
    DateTime Timestamp,
    string TimeAgo,             // "2 minutes ago", "1 hour ago"
    
    // Additional info
    string? IpAddress,
    string? UserAgent,
    string? Changes,            // JSON of changes for updates
    
    // Visual
    string Icon,
    string Color
);

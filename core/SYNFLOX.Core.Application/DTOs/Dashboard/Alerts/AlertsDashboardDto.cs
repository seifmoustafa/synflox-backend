using Application.DTOs.Dashboard.Shared;

namespace Application.DTOs.Dashboard.Alerts;

/// <summary>
/// Alerts dashboard response with all system alerts
/// </summary>
public record AlertsDashboardDto(
    // Summary
    AlertCountsDto Counts,
    AlertsByCategoryDto ByCategory,
    
    // Alerts by Priority
    List<AlertItemDto> CriticalAlerts,
    List<AlertItemDto> HighPriorityAlerts,
    List<AlertItemDto> MediumPriorityAlerts,
    List<AlertItemDto> LowPriorityAlerts,
    
    // Charts
    List<DistributionItemDto> ByPriorityChart,
    List<DistributionItemDto> ByCategoryChart,
    
    // Recent dismissed
    List<AlertItemDto> RecentlyDismissed,
    
    // Quick stats
    int AlertsCreatedToday,
    int AlertsResolvedToday,
    int OverdueAlerts,
    
    // Timestamp
    DateTime GeneratedAt
);

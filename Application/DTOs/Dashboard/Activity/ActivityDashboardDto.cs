using Application.DTOs.Dashboard.Shared;

namespace Application.DTOs.Dashboard.Activity;

/// <summary>
/// Activity dashboard response with all activity analytics
/// </summary>
public record ActivityDashboardDto(
    // Summary Stats
    ActivityStatsDto Stats,
    
    // Top Admins Leaderboard
    TopAdminsDto TopAdmins,
    
    // Recent Activity Feed
    List<ActivityItemDto> RecentActivity,
    int TotalActivityCount,
    
    // Activity Breakdown
    ActivityBreakdownDto Breakdown,
    List<DistributionItemDto> ByEntityTypeChart,
    List<DistributionItemDto> ByActionTypeChart,
    
    // Timeline
    ActivityTimelineDto Timeline,
    
    // Timestamp
    DateTime GeneratedAt
);

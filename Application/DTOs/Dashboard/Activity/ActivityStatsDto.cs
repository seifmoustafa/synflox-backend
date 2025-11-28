namespace Application.DTOs.Dashboard.Activity;

/// <summary>
/// Activity statistics summary
/// </summary>
public record ActivityStatsDto(
    // Login stats
    int TotalLogins,
    int TodayLogins,
    int ThisWeekLogins,
    int ThisMonthLogins,
    int UniqueAdminsToday,
    
    // Action stats
    int TotalActions,
    int TodayActions,
    int ThisWeekActions,
    int ThisMonthActions,
    
    // Session stats
    int ActiveSessions,
    decimal AverageSessionDuration,     // in minutes
    
    // Comparison
    int PreviousPeriodLogins,
    int PreviousPeriodActions,
    decimal LoginChangePercentage,
    decimal ActionChangePercentage
);

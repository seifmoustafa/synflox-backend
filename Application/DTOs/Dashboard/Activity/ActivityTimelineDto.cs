namespace Application.DTOs.Dashboard.Activity;

/// <summary>
/// Activity timeline data point
/// </summary>
public record ActivityTimelinePointDto(
    DateTime Date,
    string Label,
    int Logins,
    int Actions,
    int UniqueAdmins
);

/// <summary>
/// Hourly activity distribution
/// </summary>
public record HourlyActivityDto(
    int Hour,
    string HourLabel,           // "9:00 AM", "2:00 PM"
    int ActivityCount,
    decimal Percentage
);

/// <summary>
/// Activity timeline summary
/// </summary>
public record ActivityTimelineDto(
    List<ActivityTimelinePointDto> DailyData,
    List<HourlyActivityDto> HourlyDistribution,
    int PeakHour,
    string PeakHourLabel,
    int PeakHourActivity,
    string MostActiveDayOfWeek
);

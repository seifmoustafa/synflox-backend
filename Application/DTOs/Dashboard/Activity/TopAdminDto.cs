namespace Application.DTOs.Dashboard.Activity;

/// <summary>
/// Top admin by activity
/// </summary>
public record TopAdminDto(
    Guid AdminId,
    string FullName,
    string Username,
    string? ProfilePictureUrl,
    string AdminType,
    int TotalActions,
    int TodayActions,
    int LoginCount,
    DateTime LastLogin,
    DateTime LastAction,
    int Rank
);

/// <summary>
/// Top admins leaderboard
/// </summary>
public record TopAdminsDto(
    List<TopAdminDto> Admins,
    int TotalActiveAdmins,
    decimal AverageActionsPerAdmin
);

namespace Application.DTOs.Dashboard.Activity;

/// <summary>
/// Activity breakdown by entity type
/// </summary>
public record ActivityByEntityTypeDto(
    string EntityType,
    int Count,
    decimal Percentage,
    string Color
);

/// <summary>
/// Activity breakdown by action type
/// </summary>
public record ActivityByActionTypeDto(
    string ActionType,
    int Count,
    decimal Percentage,
    string Color
);

/// <summary>
/// Activity breakdown summary
/// </summary>
public record ActivityBreakdownDto(
    List<ActivityByEntityTypeDto> ByEntityType,
    List<ActivityByActionTypeDto> ByActionType,
    string MostActiveEntityType,
    string MostCommonAction
);

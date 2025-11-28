namespace Application.DTOs.Dashboard.Overview;

/// <summary>
/// KPI card data for dashboard overview
/// </summary>
public record KpiCardDto(
    string Title,
    int Value,
    int? PreviousValue,
    decimal? ChangePercentage,
    string? ChangeDirection, // "up", "down", "unchanged"
    string Icon,
    string Color
);

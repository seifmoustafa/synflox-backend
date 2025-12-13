namespace Application.DTOs.Dashboard.Overview;

/// <summary>
/// KPI card data for dashboard overview
/// </summary>
public record KpiCardDto(
    string Title,
    decimal Value, // Changed from int to decimal to support large revenue values
    decimal? PreviousValue, // Changed from int? to decimal? for consistency
    decimal? ChangePercentage,
    string? ChangeDirection, // "up", "down", "unchanged"
    string Icon,
    string Color,
    string? CurrencySymbol = null // Optional currency symbol for monetary values
);

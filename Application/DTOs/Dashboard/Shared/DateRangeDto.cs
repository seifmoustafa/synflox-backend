namespace Application.DTOs.Dashboard.Shared;

/// <summary>
/// Represents a date range for filtering dashboard data
/// </summary>
public record DateRangeDto(
    DateTime StartDate,
    DateTime EndDate
);

namespace Application.DTOs.Dashboard.Shared;

/// <summary>
/// Generic chart data point for line/bar charts
/// </summary>
public record ChartDataPointDto(
    string Label,
    decimal Value
);

/// <summary>
/// Time-series data point with date
/// </summary>
public record TimeSeriesDataPointDto(
    DateTime Date,
    string Label,
    decimal Value
);

/// <summary>
/// Growth trend data point with both companies and subscriptions
/// </summary>
public record GrowthTrendDataPointDto(
    DateTime Date,
    string Label,
    int Companies,
    int Subscriptions
);

/// <summary>
/// Distribution item for pie/doughnut charts
/// </summary>
public record DistributionItemDto(
    string Name,
    int Count,
    decimal Percentage,
    string Color
);

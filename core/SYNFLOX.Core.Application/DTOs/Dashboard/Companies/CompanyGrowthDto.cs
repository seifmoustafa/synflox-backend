namespace Application.DTOs.Dashboard.Companies;

/// <summary>
/// Company growth data point
/// </summary>
public record CompanyGrowthPointDto(
    DateTime Date,
    int NewCompanies,
    int TotalCompanies
);

/// <summary>
/// Company growth summary
/// </summary>
public record CompanyGrowthSummaryDto(
    int TotalGrowth,            // New companies in period
    decimal GrowthRate,         // Percentage growth
    int AveragePerDay,
    int BestDay,
    DateTime BestDayDate,
    List<CompanyGrowthPointDto> DailyData
);

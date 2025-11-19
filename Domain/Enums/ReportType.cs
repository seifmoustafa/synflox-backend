namespace Domain.Enums;

/// <summary>
/// Security report types
/// </summary>
public enum ReportType
{
    /// <summary>
    /// Summary report with key metrics
    /// </summary>
    Summary = 0,

    /// <summary>
    /// Detailed report with comprehensive data
    /// </summary>
    Detailed = 1,

    /// <summary>
    /// Audit report for compliance
    /// </summary>
    Audit = 2,

    /// <summary>
    /// Threat analysis report
    /// </summary>
    Threat = 3
}

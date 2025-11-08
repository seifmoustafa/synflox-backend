namespace Domain.Enums;

/// <summary>
/// Types of system metrics that can be tracked.
/// </summary>
public enum MetricType
{
    /// <summary>
    /// Total number of API requests.
    /// </summary>
    RequestCount = 1,

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    ResponseTime = 2,

    /// <summary>
    /// Error rate as a percentage.
    /// </summary>
    ErrorRate = 3,

    /// <summary>
    /// Number of active companies.
    /// </summary>
    ActiveCompanies = 4,

    /// <summary>
    /// Number of expired companies.
    /// </summary>
    ExpiredCompanies = 5,

    /// <summary>
    /// Number of suspended companies.
    /// </summary>
    SuspendedCompanies = 6,

    /// <summary>
    /// Total number of API keys.
    /// </summary>
    ApiKeyCount = 7,

    /// <summary>
    /// Database query execution time in milliseconds.
    /// </summary>
    DatabaseQueryTime = 8,

    /// <summary>
    /// Memory usage in MB.
    /// </summary>
    MemoryUsage = 9,

    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    CpuUsage = 10
}




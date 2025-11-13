namespace Domain.Enums;

/// <summary>
/// Types of client API endpoints available
/// </summary>
public enum ClientEndpointType
{
    /// <summary>
    /// Subscription status query endpoints
    /// </summary>
    SubscriptionStatus = 1,

    /// <summary>
    /// License key validation endpoints
    /// </summary>
    LicenseValidation = 2,

    /// <summary>
    /// Usage statistics and analytics
    /// </summary>
    UsageStatistics = 3,

    /// <summary>
    /// Company profile information
    /// </summary>
    CompanyProfile = 4,

    /// <summary>
    /// Token validation and health check
    /// </summary>
    TokenValidation = 5,

    /// <summary>
    /// Plan features and capabilities
    /// </summary>
    PlanFeatures = 6,

    /// <summary>
    /// Health check endpoint
    /// </summary>
    HealthCheck = 7,

    /// <summary>
    /// Subscription history
    /// </summary>
    SubscriptionHistory = 8,

    /// <summary>
    /// API documentation endpoint
    /// </summary>
    Documentation = 9
}

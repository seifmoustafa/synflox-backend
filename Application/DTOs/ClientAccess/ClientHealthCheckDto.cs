using System;
using System.Collections.Generic;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Health check response for client systems
/// Provides system status and connectivity information
/// </summary>
public class ClientHealthCheckDto
{
    /// <summary>
    /// Overall system health status
    /// </summary>
    public string Status { get; set; } = "Healthy"; // Healthy, Warning, Critical

    /// <summary>
    /// Timestamp of the health check
    /// </summary>
    public DateTime CheckTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Company and subscription health
    /// </summary>
    public ClientCompanyHealthDto Company { get; set; } = new();
    public List<ClientSubscriptionHealthDto> Subscriptions { get; set; } = new();

    /// <summary>
    /// API service health
    /// </summary>
    public ClientApiHealthDto ApiHealth { get; set; } = new();

    /// <summary>
    /// License system health
    /// </summary>
    public ClientLicenseHealthDto LicenseHealth { get; set; } = new();

    /// <summary>
    /// System information
    /// </summary>
    public string SystemVersion { get; set; } = string.Empty;
    public DateTime SystemTime { get; set; } = DateTime.UtcNow;
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Alerts and warnings
    /// </summary>
    public List<ClientHealthAlertDto> Alerts { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Company health information
/// </summary>
public class ClientCompanyHealthDto
{
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ActiveSubscriptions { get; set; }
    public int ActiveTokens { get; set; }
    public DateTime? LastActivity { get; set; }
}

/// <summary>
/// Subscription health information
/// </summary>
public class ClientSubscriptionHealthDto
{
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasValidLicense { get; set; }
}

/// <summary>
/// API health information
/// </summary>
public class ClientApiHealthDto
{
    public bool IsAvailable { get; set; } = true;
    public double ResponseTimeMs { get; set; }
    public int RateLimitRemaining { get; set; }
    public DateTime RateLimitReset { get; set; }
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// License system health
/// </summary>
public class ClientLicenseHealthDto
{
    public bool IsAvailable { get; set; } = true;
    public int ValidLicenses { get; set; }
    public int ExpiredLicenses { get; set; }
    public DateTime? LastValidation { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Health alert information
/// </summary>
public class ClientHealthAlertDto
{
    public string Level { get; set; } = string.Empty; // Info, Warning, Error, Critical
    public string Message { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ActionRequired { get; set; }
}

using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Client-facing subscription status information
/// Contains only information that external clients should see
/// </summary>
public class ClientSubscriptionStatusDto
{
    /// <summary>
    /// Company information
    /// </summary>
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Subscription information
    /// </summary>
    public Guid SubscriptionId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanDescription { get; set; } = string.Empty;

    /// <summary>
    /// Subscription status
    /// </summary>
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsTrial { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string StatusReason { get; set; } = string.Empty;

    /// <summary>
    /// Plan features and capabilities
    /// </summary>
    public List<string> Features { get; set; } = new();
    public List<string> Modules { get; set; } = new();
    public Dictionary<string, object> PlanLimits { get; set; } = new();

    /// <summary>
    /// License key information
    /// </summary>
    public bool HasLicenseKey { get; set; }
    public DateTime? LicenseKeyGeneratedAt { get; set; }
    public int LicenseKeyVersion { get; set; }

    /// <summary>
    /// Usage information (if available)
    /// </summary>
    public Dictionary<string, int> UsageStatistics { get; set; } = new();

    /// <summary>
    /// Next scheduled subscription (for deferred upgrades)
    /// </summary>
    public bool AutoRenew { get; set; }
    public Guid? NextSubscriptionId { get; set; }
    public string? NextSubscriptionPlanName { get; set; }
    public DateTime? NextSubscriptionActivationDateUtc { get; set; }

    /// <summary>
    /// Computed status message
    /// </summary>
    public string StatusMessage => IsExpired ? "Subscription has expired" :
                                  !IsActive ? "Subscription is inactive" :
                                  DaysUntilExpiry <= 7 ? $"Subscription expires in {DaysUntilExpiry} days" :
                                  IsTrial ? "Trial subscription active" :
                                  "Subscription is active";

    /// <summary>
    /// Health status for client systems
    /// </summary>
    public string HealthStatus => IsActive && !IsExpired ? "Healthy" : "Warning";
}

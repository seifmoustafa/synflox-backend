using System;
using System.Collections.Generic;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Subscription history information for client view
/// </summary>
public class ClientSubscriptionHistoryDto
{
    /// <summary>
    /// Company information
    /// </summary>
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// History period
    /// </summary>
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalMonths { get; set; }

    /// <summary>
    /// Subscription history entries
    /// </summary>
    public List<ClientSubscriptionHistoryEntryDto> History { get; set; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public ClientSubscriptionSummaryDto Summary { get; set; } = new();

    /// <summary>
    /// Current active subscriptions
    /// </summary>
    public List<ClientActiveSubscriptionDto> ActiveSubscriptions { get; set; } = new();
}

/// <summary>
/// Individual subscription history entry
/// </summary>
public class ClientSubscriptionHistoryEntryDto
{
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool WasTrial { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? EndReason { get; set; }
    public int DurationDays { get; set; }
    public bool AutoRenewed { get; set; }
    public string? UpgradedToPlan { get; set; }
}

/// <summary>
/// Subscription summary statistics
/// </summary>
public class ClientSubscriptionSummaryDto
{
    public int TotalSubscriptions { get; set; }
    public int CompletedSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TrialSubscriptions { get; set; }
    public int UpgradedSubscriptions { get; set; }
    public int TotalDaysSubscribed { get; set; }
    public DateTime FirstSubscriptionDate { get; set; }
    public string MostUsedPlan { get; set; } = string.Empty;
    public double AverageSubscriptionDuration { get; set; }
}

/// <summary>
/// Current active subscription information
/// </summary>
public class ClientActiveSubscriptionDto
{
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsTrial { get; set; }
    public bool AutoRenew { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasLicenseKey { get; set; }
}

using System;
using System.Collections.Generic;

namespace Application.DTOs.ClientAdmin;

/// <summary>
/// Summary of all devices for a company.
/// </summary>
public class CompanyDeviceSummary
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TotalSubscriptions { get; set; }
    public int SubscriptionsWithDeviceLimit { get; set; }
    public int TotalBoundDevices { get; set; }
    public int TotalMaxDevices { get; set; }
    public List<SubscriptionDeviceSummary> Subscriptions { get; set; } = new();
}

/// <summary>
/// Device summary for a single subscription.
/// </summary>
public class SubscriptionDeviceSummary
{
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int MaxDevices { get; set; }
    public int BoundDevices { get; set; }
    public int RemainingSlots { get; set; }
    public bool RequiresMachineBinding { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
}

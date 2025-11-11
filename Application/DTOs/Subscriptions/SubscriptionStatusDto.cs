using System;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// Comprehensive status information for a subscription
/// </summary>
public class SubscriptionStatusDto
{
    public Guid SubscriptionId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    
    // Status flags
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsTrial { get; set; }
    
    // Dates
    public DateTime StartDateUtc { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public int GracePeriodDays { get; set; }
    public DateTime GraceEndDateUtc { get; set; }
    
    // Next plan (for deferred upgrades)
    public Guid? NextPlanId { get; set; }
    public string? NextPlanName { get; set; }
    public DateTime? NextPlanStartDateUtc { get; set; }
    
    // Status reason
    public string? StatusReason { get; set; }
    
    // Commercial info
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    
    // Computed status message (localized)
    public string StatusMessage { get; set; } = string.Empty;
}

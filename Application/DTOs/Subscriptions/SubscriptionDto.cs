using System;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsTrial { get; set; }
    public bool IsExpired { get; set; }
    public bool AutoRenew { get; set; }
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    public string? StatusReason { get; set; }
    public Guid? NextPlanId { get; set; }
    public string? NextPlanName { get; set; }
    public DateTime? NextPlanStartDateUtc { get; set; }
    
    // Offline License Key Management
    public string? OfflineLicenseKey { get; set; }
    public DateTime? LicenseKeyGeneratedAt { get; set; }
    public int LicenseKeyVersion { get; set; }
    public bool HasLicenseKey => !string.IsNullOrEmpty(OfflineLicenseKey);
}

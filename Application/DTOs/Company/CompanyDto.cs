using System;

namespace Application.DTOs.Company;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? LicenseKey { get; set; }
    public bool IsTrial { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}


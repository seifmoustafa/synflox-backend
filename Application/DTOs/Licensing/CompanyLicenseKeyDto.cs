using System;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for company license key information
/// Shows all license keys for a company across subscriptions
/// </summary>
public class CompanyLicenseKeyDto
{
    /// <summary>
    /// Encrypted subscription ID
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted plan ID
    /// </summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// Plan name for display
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// The offline license key (only shown to SuperAdmin)
    /// </summary>
    public string? LicenseKey { get; set; }

    /// <summary>
    /// When the license key was generated
    /// </summary>
    public DateTime? GeneratedAt { get; set; }

    /// <summary>
    /// When the subscription expires
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Whether the subscription is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the subscription has expired
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// License key version
    /// </summary>
    public int KeyVersion { get; set; }

    /// <summary>
    /// Status of the license key
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

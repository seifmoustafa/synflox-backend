using System;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// License information for admin display.
/// Used in subscription details and company license management.
/// </summary>
public class OfflineLicenseDto
{
    /// <summary>
    /// Unique license identifier
    /// </summary>
    public Guid LicenseId { get; set; }

    /// <summary>
    /// Subscription ID (encrypted for response)
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Company ID (encrypted for response)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Company name for display
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Plan name for display
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// The actual license key (only shown to SuperAdmin)
    /// </summary>
    public string? LicenseKey { get; set; }

    /// <summary>
    /// When the license key was generated
    /// </summary>
    public DateTime? GeneratedAtUtc { get; set; }

    /// <summary>
    /// When the license expires
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Days until expiry
    /// </summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// License key format version
    /// </summary>
    public int KeyVersion { get; set; }

    /// <summary>
    /// Entitlements version
    /// </summary>
    public int EntitlementsVersion { get; set; }

    /// <summary>
    /// Whether the subscription is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the license has expired
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Whether the license is machine-bound
    /// </summary>
    public bool IsMachineBound { get; set; }

    /// <summary>
    /// Hash of bound machine fingerprint (truncated for display)
    /// </summary>
    public string? MachineFingerprint { get; set; }

    /// <summary>
    /// Number of authorized machines
    /// </summary>
    public int AuthorizedMachineCount { get; set; }

    /// <summary>
    /// Current access mode
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubscriptionAccessMode AccessMode { get; set; }

    /// <summary>
    /// License status for display
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Status color for UI (green, yellow, orange, red, gray)
    /// </summary>
    public string StatusColor { get; set; } = "gray";

    /// <summary>
    /// Whether a valid license key exists
    /// </summary>
    public bool HasValidKey { get; set; }

    /// <summary>
    /// Whether the license can be regenerated
    /// </summary>
    public bool CanRegenerate { get; set; }

    /// <summary>
    /// Whether the license can be revoked
    /// </summary>
    public bool CanRevoke { get; set; }

    /// <summary>
    /// Last validation timestamp (if tracked)
    /// </summary>
    public DateTime? LastValidationUtc { get; set; }

    /// <summary>
    /// Total validation count (if tracked)
    /// </summary>
    public int ValidationCount { get; set; }
}

/// <summary>
/// Summary of all licenses for a company
/// </summary>
public class CompanyLicenseSummaryDto
{
    /// <summary>
    /// Company ID (encrypted)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Company name
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Total licenses for this company
    /// </summary>
    public int TotalLicenses { get; set; }

    /// <summary>
    /// Active licenses count
    /// </summary>
    public int ActiveLicenses { get; set; }

    /// <summary>
    /// Expired licenses count
    /// </summary>
    public int ExpiredLicenses { get; set; }

    /// <summary>
    /// Licenses expiring within 30 days
    /// </summary>
    public int ExpiringLicenses { get; set; }

    /// <summary>
    /// Individual license details
    /// </summary>
    public System.Collections.Generic.List<OfflineLicenseDto> Licenses { get; set; } = new();
}

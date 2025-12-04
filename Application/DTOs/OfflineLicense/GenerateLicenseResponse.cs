using System;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// Response containing the generated offline license key.
/// </summary>
public class GenerateLicenseResponse
{
    /// <summary>
    /// The generated license key (Base64Url encoded encrypted payload)
    /// </summary>
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// Unique license identifier for tracking
    /// </summary>
    public Guid LicenseId { get; set; }

    /// <summary>
    /// When this license key was generated
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>
    /// When this license expires
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Days until expiry
    /// </summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// License key format version
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Entitlements version (for client cache invalidation)
    /// </summary>
    public int EntitlementsVersion { get; set; }

    /// <summary>
    /// Whether this key is bound to a specific machine
    /// </summary>
    public bool IsMachineBound { get; set; }

    /// <summary>
    /// Hash of the bound machine fingerprint (for verification)
    /// </summary>
    public string? MachineFingerprint { get; set; }

    /// <summary>
    /// Company name for display
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Plan name for display
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

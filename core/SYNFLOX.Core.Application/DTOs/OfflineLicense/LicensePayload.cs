using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// Internal structure of the encrypted license payload.
/// This is what gets encrypted inside the license key.
/// </summary>
public class LicensePayload
{
    #region Identity

    /// <summary>
    /// Unique license identifier (for tracking and revocation)
    /// </summary>
    [JsonPropertyName("lid")]
    public Guid LicenseId { get; set; }

    /// <summary>
    /// Company ID this license belongs to
    /// </summary>
    [JsonPropertyName("cid")]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Company name for display
    /// </summary>
    [JsonPropertyName("cn")]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Subscription ID this license is tied to
    /// </summary>
    [JsonPropertyName("sid")]
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Plan ID for reference
    /// </summary>
    [JsonPropertyName("pid")]
    public Guid PlanId { get; set; }

    /// <summary>
    /// Plan name for display
    /// </summary>
    [JsonPropertyName("pn")]
    public string PlanName { get; set; } = string.Empty;

    #endregion

    #region Hardware Binding

    /// <summary>
    /// SHA256 hash of machine fingerprint (for hardware binding)
    /// </summary>
    [JsonPropertyName("mfp")]
    public string? MachineFingerprint { get; set; }

    /// <summary>
    /// List of authorized machine fingerprints (if multi-machine)
    /// </summary>
    [JsonPropertyName("amf")]
    public List<string>? AuthorizedMachines { get; set; }

    #endregion

    #region Timestamps

    /// <summary>
    /// License issue timestamp (Unix seconds)
    /// </summary>
    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    /// <summary>
    /// License expiry timestamp (Unix seconds)
    /// </summary>
    [JsonPropertyName("exp")]
    public long ExpiresAt { get; set; }

    /// <summary>
    /// Subscription start date (Unix seconds)
    /// </summary>
    [JsonPropertyName("sta")]
    public long StartsAt { get; set; }

    /// <summary>
    /// Grace period end timestamp (Unix seconds), null if no grace
    /// </summary>
    [JsonPropertyName("gpe")]
    public long? GracePeriodEndsAt { get; set; }

    /// <summary>
    /// Export-only deadline timestamp (Unix seconds)
    /// </summary>
    [JsonPropertyName("exd")]
    public long? ExportDeadlineAt { get; set; }

    /// <summary>
    /// Last successful validation timestamp (for clock tamper detection)
    /// Updated by client application on each validation
    /// </summary>
    [JsonPropertyName("lvt")]
    public long LastValidationTime { get; set; }

    #endregion

    #region Status & Versioning

    /// <summary>
    /// License key format version
    /// </summary>
    [JsonPropertyName("ver")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Entitlements version (for detecting stale licenses)
    /// </summary>
    [JsonPropertyName("ev")]
    public int EntitlementsVersion { get; set; } = 1;

    /// <summary>
    /// Whether this is a trial license
    /// </summary>
    [JsonPropertyName("trl")]
    public bool IsTrial { get; set; }

    /// <summary>
    /// Whether this license has been revoked
    /// </summary>
    [JsonPropertyName("rvk")]
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Current access mode
    /// </summary>
    [JsonPropertyName("am")]
    public SubscriptionAccessMode AccessMode { get; set; } = SubscriptionAccessMode.Full;

    #endregion

    #region Entitlements

    /// <summary>
    /// Project-level entitlements
    /// </summary>
    [JsonPropertyName("prj")]
    public List<LicenseProjectEntitlement> Projects { get; set; } = new();

    /// <summary>
    /// Standalone module entitlements (not under a project)
    /// </summary>
    [JsonPropertyName("mod")]
    public List<LicenseModuleEntitlement> StandaloneModules { get; set; } = new();

    /// <summary>
    /// Custom features list (legacy support)
    /// </summary>
    [JsonPropertyName("ftr")]
    public List<string> Features { get; set; } = new();

    #endregion

    #region Security

    /// <summary>
    /// Issuer identifier (to verify key source)
    /// </summary>
    [JsonPropertyName("iss")]
    public string Issuer { get; set; } = "SYNFLOX";

    /// <summary>
    /// Payload integrity checksum (HMAC of other fields)
    /// </summary>
    [JsonPropertyName("chk")]
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Random data for uniqueness (prevents identical keys)
    /// </summary>
    [JsonPropertyName("rnd")]
    public string RandomNonce { get; set; } = string.Empty;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets IssuedAt as DateTime
    /// </summary>
    [JsonIgnore]
    public DateTime IssuedAtUtc => DateTimeOffset.FromUnixTimeSeconds(IssuedAt).UtcDateTime;

    /// <summary>
    /// Gets ExpiresAt as DateTime
    /// </summary>
    [JsonIgnore]
    public DateTime ExpiresAtUtc => DateTimeOffset.FromUnixTimeSeconds(ExpiresAt).UtcDateTime;

    /// <summary>
    /// Gets StartsAt as DateTime
    /// </summary>
    [JsonIgnore]
    public DateTime StartsAtUtc => DateTimeOffset.FromUnixTimeSeconds(StartsAt).UtcDateTime;

    /// <summary>
    /// Gets GracePeriodEndsAt as DateTime
    /// </summary>
    [JsonIgnore]
    public DateTime? GracePeriodEndsAtUtc => GracePeriodEndsAt.HasValue 
        ? DateTimeOffset.FromUnixTimeSeconds(GracePeriodEndsAt.Value).UtcDateTime 
        : null;

    /// <summary>
    /// Gets ExportDeadlineAt as DateTime
    /// </summary>
    [JsonIgnore]
    public DateTime? ExportDeadlineAtUtc => ExportDeadlineAt.HasValue 
        ? DateTimeOffset.FromUnixTimeSeconds(ExportDeadlineAt.Value).UtcDateTime 
        : null;

    /// <summary>
    /// Calculate days until expiry
    /// </summary>
    [JsonIgnore]
    public int DaysUntilExpiry => Math.Max(0, (int)(ExpiresAtUtc - DateTime.UtcNow).TotalDays);

    /// <summary>
    /// Check if expired (past expiry date)
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;

    /// <summary>
    /// Check if in grace period
    /// </summary>
    [JsonIgnore]
    public bool IsInGracePeriod => IsExpired && GracePeriodEndsAtUtc.HasValue && DateTime.UtcNow <= GracePeriodEndsAtUtc.Value;

    /// <summary>
    /// Check if in export-only mode
    /// </summary>
    [JsonIgnore]
    public bool IsInExportOnly => IsExpired && !IsInGracePeriod && ExportDeadlineAtUtc.HasValue && DateTime.UtcNow <= ExportDeadlineAtUtc.Value;

    #endregion
}

/// <summary>
/// Project entitlement embedded in license
/// </summary>
public class LicenseProjectEntitlement
{
    [JsonPropertyName("id")]
    public Guid ProjectId { get; set; }

    [JsonPropertyName("nm")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cd")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("al")]
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;

    [JsonPropertyName("cr")]
    public bool CanCreate { get; set; } = true;

    [JsonPropertyName("rd")]
    public bool CanRead { get; set; } = true;

    [JsonPropertyName("up")]
    public bool CanUpdate { get; set; } = true;

    [JsonPropertyName("dl")]
    public bool CanDelete { get; set; } = true;

    [JsonPropertyName("ex")]
    public bool CanExport { get; set; } = true;

    [JsonPropertyName("md")]
    public List<LicenseModuleEntitlement> Modules { get; set; } = new();
}

/// <summary>
/// Module entitlement embedded in license
/// </summary>
public class LicenseModuleEntitlement
{
    [JsonPropertyName("id")]
    public Guid ModuleId { get; set; }

    [JsonPropertyName("nm")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cd")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("al")]
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;

    [JsonPropertyName("cr")]
    public bool CanCreate { get; set; } = true;

    [JsonPropertyName("rd")]
    public bool CanRead { get; set; } = true;

    [JsonPropertyName("up")]
    public bool CanUpdate { get; set; } = true;

    [JsonPropertyName("dl")]
    public bool CanDelete { get; set; } = true;

    [JsonPropertyName("ex")]
    public bool CanExport { get; set; } = true;

    [JsonPropertyName("ft")]
    public List<string> Features { get; set; } = new();
}

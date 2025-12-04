using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// Response from license validation containing full entitlement matrix.
/// This is what client applications use to determine access rights.
/// </summary>
public class ValidateLicenseResponse
{
    #region Validation Status

    /// <summary>
    /// Whether the license is valid for use
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Detailed validation status code
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OfflineLicenseValidationStatus Status { get; set; }

    /// <summary>
    /// Human-readable validation message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error message (for debugging, not for end users)
    /// </summary>
    public string? ErrorDetails { get; set; }

    #endregion

    #region Identity

    /// <summary>
    /// Company ID (for verification)
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Company name for display
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Subscription ID
    /// </summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Plan name for display
    /// </summary>
    public string? PlanName { get; set; }

    /// <summary>
    /// License unique identifier
    /// </summary>
    public Guid? LicenseId { get; set; }

    #endregion

    #region Dates & Timing

    /// <summary>
    /// When the license expires
    /// </summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Days until expiry (0 if expired)
    /// </summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// When grace period ends (if applicable)
    /// </summary>
    public DateTime? GracePeriodEndsAtUtc { get; set; }

    /// <summary>
    /// When export-only period ends (if applicable)
    /// </summary>
    public DateTime? ExportDeadlineUtc { get; set; }

    /// <summary>
    /// Whether license is currently in grace period
    /// </summary>
    public bool IsInGracePeriod { get; set; }

    /// <summary>
    /// Whether license is in export-only mode
    /// </summary>
    public bool IsInExportOnly { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Whether this is a trial license
    /// </summary>
    public bool IsTrial { get; set; }

    /// <summary>
    /// Current access mode
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubscriptionAccessMode AccessMode { get; set; }

    /// <summary>
    /// License key version
    /// </summary>
    public int KeyVersion { get; set; }

    /// <summary>
    /// Entitlements version (for client cache)
    /// </summary>
    public int EntitlementsVersion { get; set; }

    /// <summary>
    /// Whether clock tampering was detected
    /// </summary>
    public bool ClockTamperingDetected { get; set; }

    /// <summary>
    /// Whether machine fingerprint matches
    /// </summary>
    public bool MachineAuthorized { get; set; } = true;

    #endregion

    #region Entitlements

    /// <summary>
    /// Project-level entitlements with full CRUD permissions
    /// </summary>
    public List<ValidatedProjectEntitlement> Projects { get; set; } = new();

    /// <summary>
    /// Standalone module entitlements
    /// </summary>
    public List<ValidatedModuleEntitlement> StandaloneModules { get; set; } = new();

    /// <summary>
    /// Custom features list (legacy support)
    /// </summary>
    public List<string> Features { get; set; } = new();

    #endregion

    #region Warnings

    /// <summary>
    /// List of warnings (e.g., "License expires in 7 days")
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    #endregion
}

/// <summary>
/// Validated project entitlement for response
/// </summary>
public class ValidatedProjectEntitlement
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EntitlementAccessLevel AccessLevel { get; set; }

    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }

    /// <summary>
    /// Quick check for full CRUD access
    /// </summary>
    public bool HasFullAccess => CanCreate && CanRead && CanUpdate && CanDelete && CanExport;

    public List<ValidatedModuleEntitlement> Modules { get; set; } = new();
}

/// <summary>
/// Validated module entitlement for response
/// </summary>
public class ValidatedModuleEntitlement
{
    public Guid ModuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EntitlementAccessLevel AccessLevel { get; set; }

    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }

    /// <summary>
    /// Quick check for full CRUD access
    /// </summary>
    public bool HasFullAccess => CanCreate && CanRead && CanUpdate && CanDelete && CanExport;

    public List<string> Features { get; set; } = new();
}

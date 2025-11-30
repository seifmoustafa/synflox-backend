using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs.Licensing;

/// <summary>
/// Response for offline license key validation
/// Used by client applications to validate subscription access
/// Contains FULL entitlement matrix for offline systems
/// </summary>
public class LicenseKeyValidationResponse
{
    // ========== VALIDATION STATUS ==========
    
    /// <summary>
    /// Whether the license key is valid and active
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation message (error details if invalid)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    // ========== IDENTITY ==========
    
    /// <summary>
    /// Company ID (for verification)
    /// </summary>
    public Guid CompanyId { get; set; }
    
    /// <summary>
    /// Company name for display
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Plan name for display
    /// </summary>
    public string? PlanName { get; set; }

    // ========== DATES ==========
    
    /// <summary>
    /// When the subscription expires
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>
    /// When grace period ends (if in grace)
    /// </summary>
    public DateTime? GraceEndDate { get; set; }
    
    /// <summary>
    /// When export-only access ends
    /// </summary>
    public DateTime? ExportDeadline { get; set; }

    /// <summary>
    /// Days until expiry (for client display)
    /// </summary>
    public int? DaysUntilExpiry { get; set; }

    // ========== STATUS ==========
    
    /// <summary>
    /// Whether this is a trial subscription
    /// </summary>
    public bool IsTrial { get; set; }
    
    /// <summary>
    /// Current access mode (Full, GracePeriod, ExportOnly, ReadOnly, Blocked)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubscriptionAccessMode AccessMode { get; set; } = SubscriptionAccessMode.Full;
    
    /// <summary>
    /// Clock tamper detection flag
    /// </summary>
    public bool ClockTampered { get; set; }

    // ========== VERSIONING ==========
    
    /// <summary>
    /// License key version
    /// </summary>
    public int KeyVersion { get; set; }
    
    /// <summary>
    /// Entitlements version (for detecting stale licenses)
    /// </summary>
    public int EntitlementsVersion { get; set; }

    // ========== LEGACY (backward compatibility) ==========
    
    /// <summary>
    /// List of features included in this subscription
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// List of modules included in this subscription
    /// </summary>
    public List<string> Modules { get; set; } = new();

    // ========== ENTERPRISE ENTITLEMENTS ==========
    
    /// <summary>
    /// Full project-level entitlements with modules
    /// </summary>
    public List<LicenseProjectEntitlementDto> Projects { get; set; } = new();
    
    /// <summary>
    /// Standalone module entitlements
    /// </summary>
    public List<LicenseModuleEntitlementDto> StandaloneModules { get; set; } = new();
}

/// <summary>
/// Project entitlement in license validation response
/// </summary>
public class LicenseProjectEntitlementDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;
    
    public bool HasFullAccess { get; set; }
    public List<string> AllowedOperations { get; set; } = new();
    public List<LicenseModuleEntitlementDto> Modules { get; set; } = new();
}

/// <summary>
/// Module entitlement in license validation response
/// </summary>
public class LicenseModuleEntitlementDto
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;
    
    public List<string> AllowedOperations { get; set; } = new();
    public List<string> AllowedFeatures { get; set; } = new();
}

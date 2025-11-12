using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Licensing;

/// <summary>
/// Response for offline license key validation
/// Used by client applications to validate subscription access
/// </summary>
public class LicenseKeyValidationResponse
{
    /// <summary>
    /// Whether the license key is valid and active
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation message (error details if invalid)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the subscription expires
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Company name for display
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Plan name for display
    /// </summary>
    public string? PlanName { get; set; }

    /// <summary>
    /// List of features included in this subscription
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// List of modules included in this subscription
    /// </summary>
    public List<string> Modules { get; set; } = new();

    /// <summary>
    /// Whether this is a trial subscription
    /// </summary>
    public bool IsTrial { get; set; }

    /// <summary>
    /// Days until expiry (for client display)
    /// </summary>
    public int? DaysUntilExpiry { get; set; }

    /// <summary>
    /// License key version
    /// </summary>
    public int KeyVersion { get; set; }

    public bool ClockTampered { get; set; }
    public Guid CompanyId { get; set; }
}

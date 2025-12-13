using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// Request to generate a new offline license key for a subscription.
/// </summary>
public class GenerateLicenseRequest
{
    /// <summary>
    /// The subscription to generate a license for (encrypted ID from route)
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Machine fingerprint for hardware binding (optional based on settings)
    /// </summary>
    public MachineFingerprint? MachineFingerprint { get; set; }

    /// <summary>
    /// Custom notes about this license generation
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether to allow multiple machines (up to max in settings)
    /// </summary>
    public bool AllowMultipleMachines { get; set; }

    /// <summary>
    /// Whether to force regeneration even if a valid key exists
    /// </summary>
    public bool ForceRegenerate { get; set; }
}

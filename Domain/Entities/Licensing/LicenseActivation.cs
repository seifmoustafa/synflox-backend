using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Entities.Subscriptions;

namespace Domain.Entities.Licensing;

/// <summary>
/// Tracks individual device activations for a subscription license.
/// Used for:
/// - Limiting activations to N devices
/// - Machine binding verification
/// - Concurrent usage detection
/// - Audit trail of where license is used
/// </summary>
public class LicenseActivation : AuditEntity<Guid>
{
    /// <summary>
    /// The subscription this activation belongs to
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// The company this activation belongs to (denormalized for queries)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Unique hash of the machine fingerprint
    /// Created from: CPU + Motherboard + Disk + MAC (SHA256)
    /// </summary>
    [Required]
    [StringLength(128)]
    public string MachineHash { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name for the device (user-provided or auto-detected)
    /// Example: "John's Laptop", "Office PC #3"
    /// </summary>
    [StringLength(200)]
    public string? DeviceName { get; set; }

    /// <summary>
    /// Operating system info
    /// Example: "Windows 11 Pro 22H2", "macOS 14.1"
    /// </summary>
    [StringLength(100)]
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// CPU identifier (part of fingerprint)
    /// </summary>
    [StringLength(100)]
    public string? CpuId { get; set; }

    /// <summary>
    /// Motherboard serial (part of fingerprint)
    /// </summary>
    [StringLength(100)]
    public string? MotherboardSerial { get; set; }

    /// <summary>
    /// Primary disk serial (part of fingerprint)
    /// </summary>
    [StringLength(100)]
    public string? DiskSerial { get; set; }

    /// <summary>
    /// Primary MAC address (part of fingerprint)
    /// </summary>
    [StringLength(50)]
    public string? MacAddress { get; set; }

    /// <summary>
    /// When this device was first activated
    /// </summary>
    public DateTime ActivatedAtUtc { get; set; }

    /// <summary>
    /// Last time this device was seen (heartbeat/validation)
    /// Used for concurrent usage detection and cleanup
    /// </summary>
    public DateTime LastSeenAtUtc { get; set; }

    /// <summary>
    /// Last IP address this device connected from
    /// </summary>
    [StringLength(50)]
    public string? LastIpAddress { get; set; }

    /// <summary>
    /// Whether this activation is currently active
    /// False if deactivated/revoked
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reason for deactivation (if deactivated)
    /// </summary>
    [StringLength(500)]
    public string? DeactivationReason { get; set; }

    /// <summary>
    /// When this device was deactivated
    /// </summary>
    public DateTime? DeactivatedAtUtc { get; set; }

    /// <summary>
    /// Hardware change tolerance counter
    /// Incremented when fingerprint partially matches
    /// If exceeds threshold, activation is invalidated
    /// </summary>
    public int HardwareChangeCount { get; set; }

    /// <summary>
    /// Last hardware change detected
    /// </summary>
    public DateTime? LastHardwareChangeAtUtc { get; set; }

    /// <summary>
    /// Total number of validations from this device
    /// </summary>
    public int ValidationCount { get; set; }

    /// <summary>
    /// User agent string from last validation
    /// </summary>
    [StringLength(500)]
    public string? LastUserAgent { get; set; }

    // Navigation properties
    public Subscription Subscription { get; set; } = null!;
    public Company Company { get; set; } = null!;
}

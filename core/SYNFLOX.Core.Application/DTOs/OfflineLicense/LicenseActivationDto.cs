using System;
using Domain.Enums;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// DTO for license activation information
/// </summary>
public class LicenseActivationDto
{
    /// <summary>
    /// Activation ID
    /// </summary>
    public Guid ActivationId { get; set; }

    /// <summary>
    /// Device name (user-friendly)
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Operating system
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// Truncated machine hash (for display)
    /// </summary>
    public string MachineHashTruncated { get; set; } = string.Empty;

    /// <summary>
    /// When device was activated
    /// </summary>
    public DateTime ActivatedAtUtc { get; set; }

    /// <summary>
    /// Last activity from this device
    /// </summary>
    public DateTime LastSeenAtUtc { get; set; }

    /// <summary>
    /// Last IP address
    /// </summary>
    public string? LastIpAddress { get; set; }

    /// <summary>
    /// Whether this activation is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of times this device validated
    /// </summary>
    public int ValidationCount { get; set; }

    /// <summary>
    /// Hardware changes detected
    /// </summary>
    public int HardwareChangeCount { get; set; }
}

/// <summary>
/// Response for activation attempt
/// </summary>
public class ActivationResponse
{
    /// <summary>
    /// Whether activation was successful
    /// </summary>
    public bool IsActivated { get; set; }

    /// <summary>
    /// Message describing result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Current activation count
    /// </summary>
    public int CurrentActivations { get; set; }

    /// <summary>
    /// Maximum allowed activations
    /// </summary>
    public int MaxActivations { get; set; }

    /// <summary>
    /// Remaining activation slots
    /// </summary>
    public int RemainingActivations => MaxActivations == 0 ? int.MaxValue : MaxActivations - CurrentActivations;

    /// <summary>
    /// List of currently activated devices
    /// </summary>
    public List<LicenseActivationDto> ActivatedDevices { get; set; } = new();

    /// <summary>
    /// Warnings (e.g., hardware change detected)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Activation ID (if successful)
    /// </summary>
    public Guid? ActivationId { get; set; }

    /// <summary>
    /// Whether concurrent usage was detected
    /// </summary>
    public bool ConcurrentUsageDetected { get; set; }

    /// <summary>
    /// Other devices currently in use (if concurrent usage detected)
    /// </summary>
    public List<LicenseActivationDto> OtherActiveDevices { get; set; } = new();
}

/// <summary>
/// Request to activate a device
/// </summary>
public class ActivateDeviceRequest
{
    /// <summary>
    /// Machine fingerprint
    /// </summary>
    public MachineFingerprint MachineFingerprint { get; set; } = new();

    /// <summary>
    /// Friendly device name
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Operating system info
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// Force activation even if max devices reached
    /// (will deactivate oldest device)
    /// </summary>
    public bool ForceActivation { get; set; }
}

/// <summary>
/// Request to deactivate a device
/// </summary>
public class DeactivateDeviceRequest
{
    /// <summary>
    /// Activation ID to deactivate
    /// </summary>
    public Guid ActivationId { get; set; }

    /// <summary>
    /// Reason for deactivation
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Summary of subscription's device activations
/// </summary>
public class ActivationSummaryDto
{
    /// <summary>
    /// Subscription ID
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Company name
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Plan name
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum devices allowed
    /// </summary>
    public int MaxDevices { get; set; }

    /// <summary>
    /// Current active devices
    /// </summary>
    public int ActiveDeviceCount { get; set; }

    /// <summary>
    /// Whether machine binding is required
    /// </summary>
    public bool RequireMachineBinding { get; set; }

    /// <summary>
    /// How concurrent device access is handled
    /// </summary>
    public ConcurrentAccessMode ConcurrentAccessMode { get; set; }
    
    /// <summary>
    /// Maximum concurrent devices allowed
    /// </summary>
    public int MaxConcurrentDevices { get; set; }
    
    /// <summary>
    /// Heartbeat timeout in minutes
    /// </summary>
    public int DeviceHeartbeatTimeoutMinutes { get; set; }

    /// <summary>
    /// Hardware change tolerance
    /// </summary>
    public int HardwareChangeTolerance { get; set; }

    /// <summary>
    /// List of activated devices
    /// </summary>
    public List<LicenseActivationDto> Activations { get; set; } = new();
}

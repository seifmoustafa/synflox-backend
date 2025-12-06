using System;
using System.Collections.Generic;
using Application.DTOs.OfflineLicense;

namespace Application.DTOs.ClientAdmin;

/// <summary>
/// Request to bind a device.
/// </summary>
public class BindDeviceRequest
{
    public Guid SubscriptionId { get; set; }
    public required MachineFingerprint MachineFingerprint { get; set; }
    public string? DeviceName { get; set; }
    public string? OperatingSystem { get; set; }
}

/// <summary>
/// Request to bind multiple devices.
/// </summary>
public class BulkBindDeviceRequest
{
    public Guid SubscriptionId { get; set; }
    public required List<DeviceInfo> Devices { get; set; }
}

/// <summary>
/// Device info for bulk binding.
/// </summary>
public class DeviceInfo
{
    public required MachineFingerprint MachineFingerprint { get; set; }
    public string? DeviceName { get; set; }
    public string? OperatingSystem { get; set; }
}

/// <summary>
/// Response from device binding.
/// </summary>
public class DeviceBindingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ActivationId { get; set; }
    public int CurrentDeviceCount { get; set; }
    public int MaxDevices { get; set; }
    public int RemainingSlots { get; set; }
    public List<string>? Warnings { get; set; }
}

/// <summary>
/// Response from bulk device binding.
/// </summary>
public class BulkDeviceBindingResponse
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<DeviceBindingResult> Results { get; set; } = new();
}

/// <summary>
/// Individual result in bulk binding.
/// </summary>
public class DeviceBindingResult
{
    public string DeviceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? ActivationId { get; set; }
}

/// <summary>
/// Request to unbind a device.
/// </summary>
public class UnbindDeviceRequest
{
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Either ActivationId or MachineFingerprint must be provided.
    /// </summary>
    public Guid? ActivationId { get; set; }
    public MachineFingerprint? MachineFingerprint { get; set; }
    
    public string? Reason { get; set; }
}

/// <summary>
/// Bound device information.
/// </summary>
public class BoundDeviceDto
{
    public Guid ActivationId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string? OperatingSystem { get; set; }
    public string MachineHashTruncated { get; set; } = string.Empty;
    public DateTime ActivatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public int ValidationCount { get; set; }
    public bool IsActive { get; set; }
}

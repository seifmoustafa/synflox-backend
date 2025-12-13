using System;
using Application.DTOs.OfflineLicense;

namespace Application.DTOs.ClientAdmin;

/// <summary>
/// Device replacement request information.
/// </summary>
public class DeviceReplacementRequestDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    
    // New device info
    public string NewDeviceName { get; set; } = string.Empty;
    public string? NewDeviceOs { get; set; }
    public string NewMachineHashTruncated { get; set; } = string.Empty;
    
    // Old device to replace
    public Guid? OldActivationId { get; set; }
    public string? OldDeviceName { get; set; }
    
    public DateTime RequestedAtUtc { get; set; }
    public string RequestedFromIp { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a device replacement.
/// </summary>
public class CreateReplacementRequest
{
    public Guid SubscriptionId { get; set; }
    public required MachineFingerprint MachineFingerprint { get; set; }
    public string? DeviceName { get; set; }
    public string? OperatingSystem { get; set; }
    
    /// <summary>
    /// Optional: Specific device to replace. If not provided, uses plan policy.
    /// </summary>
    public Guid? OldActivationId { get; set; }
    
    /// <summary>
    /// IP address of the request (set by controller).
    /// </summary>
    public string? RequestedFromIp { get; set; }
    
    /// <summary>
    /// User agent of the request (set by controller).
    /// </summary>
    public string? RequestedUserAgent { get; set; }
}

/// <summary>
/// Response from creating a replacement request.
/// </summary>
public class CreateReplacementResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? RequestId { get; set; }
    
    /// <summary>
    /// If device is already activated, no need for replacement.
    /// </summary>
    public bool AlreadyActivated { get; set; }
    
    /// <summary>
    /// If replacement request already pending for this device.
    /// </summary>
    public bool AlreadyPending { get; set; }
    
    /// <summary>
    /// Suggested device name to be replaced based on policy.
    /// </summary>
    public string? SuggestedDeviceToReplace { get; set; }
    
    /// <summary>
    /// When the request expires.
    /// </summary>
    public DateTime? ExpiresAtUtc { get; set; }
}

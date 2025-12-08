using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs.OnlineAccess;

/// <summary>
/// DTO for OnlineDeviceBinding display.
/// </summary>
public class OnlineDeviceDto
{
    public Guid Id { get; set; }
    public Guid TokenId { get; set; }
    public Guid SubscriptionId { get; set; }
    public required string DeviceFingerprint { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? OperatingSystem { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OnlineDeviceStatus Status { get; set; }
    public DateTime FirstSeenAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public string? LastIpAddress { get; set; }
    public long ApiCallCount { get; set; }
    public string? StatusReason { get; set; }
}

/// <summary>
/// Request DTO for registering a device.
/// </summary>
public class RegisterDeviceRequest
{
    /// <summary>
    /// Unique device fingerprint (hardware ID, UUID, etc.).
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 8)]
    public required string DeviceFingerprint { get; set; }
    
    /// <summary>
    /// Human-readable device name (optional).
    /// </summary>
    [StringLength(100)]
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// Device type (Desktop, Mobile, Tablet, Server, etc.).
    /// </summary>
    [StringLength(50)]
    public string? DeviceType { get; set; }
    
    /// <summary>
    /// Operating system (Windows, macOS, Linux, Android, iOS, etc.).
    /// </summary>
    [StringLength(100)]
    public string? OperatingSystem { get; set; }
}

/// <summary>
/// Response DTO for device registration.
/// Note: DeviceId should be encrypted before returning to client.
/// </summary>
public class RegisterDeviceResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    /// <summary>
    /// Encrypted device ID. Use AutoMapper to encrypt before returning.
    /// </summary>
    public Guid? DeviceId { get; set; }
    public bool LimitReached { get; set; }
    public int CurrentDeviceCount { get; set; }
    public int? MaxDevicesAllowed { get; set; }
    
    /// <summary>
    /// Whether admin approval is required to register this device.
    /// </summary>
    public bool RequiresAdminApproval { get; set; }
}

/// <summary>
/// DTO for device limit information.
/// </summary>
public class DeviceLimitDto
{
    public int CurrentCount { get; set; }
    public int? MaxAllowed { get; set; }
    public bool IsUnlimited { get; set; }
    
    /// <summary>
    /// Whether new devices require admin approval before registration.
    /// </summary>
    public bool RequiresAdminApproval { get; set; }
    
    /// <summary>
    /// The device admission mode (Open, AdminOnly, AutoWithQueue, HybridAutoAdmin).
    /// </summary>
    public string? AdmissionMode { get; set; }
    
    public bool CanRegisterMore => (IsUnlimited || CurrentCount < MaxAllowed) && !RequiresAdminApproval;
    public int? RemainingSlots => IsUnlimited ? null : MaxAllowed - CurrentCount;
}

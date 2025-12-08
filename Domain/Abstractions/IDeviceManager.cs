using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Abstractions;

/// <summary>
/// Base interface for device management operations.
/// Shared by both Online and Offline systems.
/// </summary>
public interface IDeviceManager
{
    /// <summary>
    /// Register a new device for a subscription.
    /// </summary>
    Task<DeviceRegistrationResult> RegisterDeviceAsync(
        Guid subscriptionId,
        string deviceFingerprint,
        string? deviceName,
        string? deviceType,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unregister a device from a subscription.
    /// </summary>
    Task<bool> UnregisterDeviceAsync(
        Guid subscriptionId,
        string deviceFingerprint,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all registered devices for a subscription.
    /// </summary>
    Task<IEnumerable<DeviceInfo>> GetDevicesAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get device count for a subscription.
    /// </summary>
    Task<int> GetDeviceCountAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a device is registered.
    /// </summary>
    Task<bool> IsDeviceRegisteredAsync(
        Guid subscriptionId,
        string deviceFingerprint,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the maximum allowed devices for a subscription.
    /// Returns null if unlimited.
    /// </summary>
    Task<int?> GetDeviceLimitAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if more devices can be registered.
    /// </summary>
    Task<bool> CanRegisterMoreDevicesAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a device registration attempt.
/// </summary>
public class DeviceRegistrationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? DeviceId { get; set; }
    public bool LimitReached { get; set; }
    public int CurrentCount { get; set; }
    public int? MaxAllowed { get; set; }
}

/// <summary>
/// Information about a registered device.
/// </summary>
public class DeviceInfo
{
    public Guid Id { get; set; }
    public required string Fingerprint { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public DateTime FirstSeenUtc { get; set; }
    public DateTime? LastSeenUtc { get; set; }
    public string? LastIpAddress { get; set; }
    public bool IsActive { get; set; }
}

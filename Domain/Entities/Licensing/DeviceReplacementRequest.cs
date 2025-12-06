using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a request to replace a bound device with a new one.
/// Created when max devices reached and a new device tries to activate.
/// Requires client admin approval before the replacement happens.
/// </summary>
public class DeviceReplacementRequest : AuditEntity<Guid>
{
    /// <summary>
    /// The subscription this replacement is for.
    /// </summary>
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Navigation property to Subscription.
    /// </summary>
    public virtual Subscription? Subscription { get; set; }

    /// <summary>
    /// The company this request belongs to.
    /// </summary>
    public Guid CompanyId { get; set; }
    
    /// <summary>
    /// Navigation property to Company.
    /// </summary>
    public virtual Company? Company { get; set; }

    #region New Device Info
    
    /// <summary>
    /// Fingerprint hash of the new device requesting activation.
    /// </summary>
    [Required]
    [StringLength(128)]
    public required string NewMachineHash { get; set; }
    
    /// <summary>
    /// Name of the new device.
    /// </summary>
    [StringLength(200)]
    public string? NewDeviceName { get; set; }
    
    /// <summary>
    /// Operating system of the new device.
    /// </summary>
    [StringLength(100)]
    public string? NewDeviceOs { get; set; }
    
    /// <summary>
    /// MAC address of the new device.
    /// </summary>
    [StringLength(50)]
    public string? NewMacAddress { get; set; }
    
    /// <summary>
    /// Motherboard serial of the new device.
    /// </summary>
    [StringLength(200)]
    public string? NewMotherboardSerial { get; set; }
    
    #endregion

    #region Old Device Info (Device to be replaced)
    
    /// <summary>
    /// The activation ID of the device that should be replaced.
    /// May be null if using auto-replace policy.
    /// </summary>
    public Guid? OldActivationId { get; set; }
    
    /// <summary>
    /// Navigation property to the old activation.
    /// </summary>
    public virtual LicenseActivation? OldActivation { get; set; }
    
    /// <summary>
    /// Name of the old device (cached for display even if activation is deleted).
    /// </summary>
    [StringLength(200)]
    public string? OldDeviceName { get; set; }
    
    /// <summary>
    /// Hash of the old device (cached).
    /// </summary>
    [StringLength(128)]
    public string? OldMachineHash { get; set; }
    
    #endregion

    #region Request Details
    
    /// <summary>
    /// Current status of the replacement request.
    /// </summary>
    public ReplacementRequestStatus Status { get; set; } = ReplacementRequestStatus.Pending;
    
    /// <summary>
    /// IP address the request came from.
    /// </summary>
    [StringLength(45)]
    public string? RequestedFromIp { get; set; }
    
    /// <summary>
    /// User agent of the requesting client.
    /// </summary>
    [StringLength(500)]
    public string? RequestedUserAgent { get; set; }
    
    /// <summary>
    /// When the request expires (auto-reject after this).
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
    
    #endregion

    #region Resolution
    
    /// <summary>
    /// When the request was resolved (approved/rejected).
    /// </summary>
    public DateTime? ResolvedAtUtc { get; set; }
    
    /// <summary>
    /// Who resolved the request (admin token ID or SYNFLOX admin ID).
    /// </summary>
    public Guid? ResolvedByTokenId { get; set; }
    
    /// <summary>
    /// Reason for rejection (if rejected).
    /// </summary>
    [StringLength(500)]
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// The new activation ID created upon approval.
    /// </summary>
    public Guid? NewActivationId { get; set; }
    
    #endregion

    // Computed properties
    
    /// <summary>
    /// Whether the request is still pending.
    /// </summary>
    public bool IsPending => Status == ReplacementRequestStatus.Pending && !IsExpired;
    
    /// <summary>
    /// Whether the request has expired.
    /// </summary>
    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;
    
    /// <summary>
    /// Hours until expiry.
    /// </summary>
    public double HoursUntilExpiry => Math.Max(0, (ExpiresAtUtc - DateTime.UtcNow).TotalHours);
}

/// <summary>
/// Status of a device replacement request.
/// </summary>
public enum ReplacementRequestStatus
{
    /// <summary>
    /// Waiting for admin approval.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Approved and new device activated.
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// Rejected by admin.
    /// </summary>
    Rejected = 2,
    
    /// <summary>
    /// Expired without action.
    /// </summary>
    Expired = 3,
    
    /// <summary>
    /// Cancelled by requester.
    /// </summary>
    Cancelled = 4
}

using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Notifications;

/// <summary>
/// Push notification subscription for FCM/APNS/Web Push.
/// Stores device tokens for sending push notifications.
/// </summary>
public class PushSubscription : BaseEntity<Guid>
{
    /// <summary>
    /// User ID (SuperAdmin or CompanyAdmin)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of user: 'SuperAdmin' or 'CompanyAdmin'
    /// </summary>
    [Required]
    [StringLength(20)]
    public string UserType { get; set; } = "SuperAdmin";

    /// <summary>
    /// Push token/endpoint from the client.
    /// FCM token, APNS token, or Web Push endpoint.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Platform type: 'web', 'android', 'ios', 'flutter', 'desktop'
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Platform { get; set; } = "web";

    /// <summary>
    /// Device name/model for display
    /// </summary>
    [StringLength(100)]
    public string? DeviceName { get; set; }

    /// <summary>
    /// Browser or app version
    /// </summary>
    [StringLength(50)]
    public string? AppVersion { get; set; }

    /// <summary>
    /// Optional device fingerprint for uniqueness
    /// </summary>
    [StringLength(100)]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// Web Push p256dh key (for web push only)
    /// </summary>
    [StringLength(200)]
    public string? P256dhKey { get; set; }

    /// <summary>
    /// Web Push auth key (for web push only)
    /// </summary>
    [StringLength(100)]
    public string? AuthKey { get; set; }

    /// <summary>
    /// When the subscription was created
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When a notification was last sent to this device
    /// </summary>
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>
    /// Last time the token was refreshed
    /// </summary>
    public DateTime? TokenRefreshedAtUtc { get; set; }

    /// <summary>
    /// Number of failed delivery attempts
    /// </summary>
    public int FailedAttempts { get; set; } = 0;

    /// <summary>
    /// Last error message if delivery failed
    /// </summary>
    [StringLength(500)]
    public string? LastError { get; set; }

    /// <summary>
    /// Record successful push
    /// </summary>
    public void RecordSuccess()
    {
        LastUsedAtUtc = DateTime.UtcNow;
        FailedAttempts = 0;
        LastError = null;
    }

    /// <summary>
    /// Record failed push attempt
    /// </summary>
    public void RecordFailure(string error)
    {
        FailedAttempts++;
        LastError = error;
        
        // Disable after 5 consecutive failures
        if (FailedAttempts >= 5)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Check if token is stale (not used in 30 days)
    /// </summary>
    public bool IsStale => LastUsedAtUtc.HasValue && 
                           (DateTime.UtcNow - LastUsedAtUtc.Value).TotalDays > 30;
}

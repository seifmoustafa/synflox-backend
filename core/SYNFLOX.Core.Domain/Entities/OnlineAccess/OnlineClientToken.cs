using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Domain.Entities.OnlineAccess;

/// <summary>
/// Represents a thin JWT token for online client systems.
/// Unlike offline licenses, this token contains NO entitlements - 
/// only identity claims (company_id, subscription_id, token_id).
/// 
/// Key Features:
/// - No token regeneration needed when plan changes
/// - Entitlements fetched dynamically via API
/// - Auto-refresh capability for seamless experience
/// - Optional device limit enforcement
/// </summary>
public class OnlineClientToken : AuditEntity<Guid>
{
    /// <summary>
    /// The company this token belongs to.
    /// </summary>
    public Guid CompanyId { get; set; }
    
    /// <summary>
    /// The subscription this token is tied to.
    /// </summary>
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Friendly name for this token (e.g., "Production API Token").
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    
    /// <summary>
    /// SHA-256 hash of the JWT token for security and lookup.
    /// We never store the actual token.
    /// </summary>
    [Required]
    [StringLength(64)]
    public required string TokenHash { get; set; }
    
    /// <summary>
    /// When the token was issued (UTC).
    /// </summary>
    public DateTime IssuedAtUtc { get; set; }
    
    /// <summary>
    /// When the token expires (UTC).
    /// Can be set to match subscription expiry or custom duration.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
    
    /// <summary>
    /// Current status of the token.
    /// </summary>
    public ClientTokenStatus Status { get; set; } = ClientTokenStatus.Active;
    
    /// <summary>
    /// Reason for revocation (if revoked).
    /// </summary>
    [StringLength(500)]
    public string? RevocationReason { get; set; }
    
    /// <summary>
    /// Auto-refresh enabled - token stays valid as long as subscription is active.
    /// When true, token doesn't need regeneration on plan changes.
    /// </summary>
    public bool AutoRefreshEnabled { get; set; } = true;
    
    /// <summary>
    /// Maximum devices allowed for this token.
    /// Null means unlimited (uses plan default).
    /// </summary>
    public int? MaxDevices { get; set; }
    
    /// <summary>
    /// When the token was last used (UTC).
    /// </summary>
    public DateTime? LastUsedAtUtc { get; set; }
    
    /// <summary>
    /// Total number of times this token has been used.
    /// </summary>
    public int UsageCount { get; set; } = 0;
    
    /// <summary>
    /// Client IP address that last used this token.
    /// </summary>
    [StringLength(45)]
    public string? LastUsedFromIp { get; set; }
    
    /// <summary>
    /// User agent of the client that last used this token.
    /// </summary>
    [StringLength(500)]
    public string? LastUsedUserAgent { get; set; }
    
    /// <summary>
    /// Optional notes for this token.
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    // ========== Computed Properties ==========
    
    /// <summary>
    /// Whether the token is currently valid (active and not expired).
    /// </summary>
    public bool IsValid => Status == ClientTokenStatus.Active && 
                          ExpiresAtUtc > DateTime.UtcNow && 
                          !IsDeleted;
    
    /// <summary>
    /// Whether the token has expired.
    /// </summary>
    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;
    
    /// <summary>
    /// Days until token expires (0 if already expired).
    /// </summary>
    public int DaysUntilExpiry => IsExpired ? 0 : (int)(ExpiresAtUtc - DateTime.UtcNow).TotalDays;
    
    // ========== Navigation Properties ==========
    
    public virtual Company Company { get; set; } = null!;
    public virtual Subscription Subscription { get; set; } = null!;
    public virtual ICollection<OnlineDeviceBinding> BoundDevices { get; set; } = new List<OnlineDeviceBinding>();
}

using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Domain.Entities.ClientAccess;

/// <summary>
/// Represents a JWT-based access token for external client systems
/// Allows companies to query their subscription status and validate licenses
/// </summary>
public class ClientAccessToken : AuditEntity<Guid>
{
    /// <summary>
    /// The company this token belongs to
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// The subscription this token is tied to
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// SHA-256 hash of the JWT token for security and lookup
    /// </summary>
    [Required]
    [StringLength(64)] // SHA-256 produces 64-character hex string
    public required string TokenHash { get; set; }

    /// <summary>
    /// When the token was issued (UTC)
    /// </summary>
    public DateTime IssuedAtUtc { get; set; }

    /// <summary>
    /// When the token expires (UTC) - typically matches subscription expiry
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Current status of the token
    /// </summary>
    public ClientTokenStatus Status { get; set; } = ClientTokenStatus.Active;

    /// <summary>
    /// Reason for revocation (if revoked)
    /// </summary>
    [StringLength(500)]
    public string? RevocationReason { get; set; }

    /// <summary>
    /// When the token was last used (UTC)
    /// </summary>
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>
    /// Total number of times this token has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Client IP address that last used this token
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? LastUsedFromIp { get; set; }

    /// <summary>
    /// User agent of the client that last used this token
    /// </summary>
    [StringLength(500)]
    public string? LastUsedUserAgent { get; set; }

    /// <summary>
    /// JSON array of allowed API endpoints for this token
    /// </summary>
    [StringLength(2000)]
    public string? AllowedEndpoints { get; set; }

    /// <summary>
    /// Token version for future compatibility
    /// </summary>
    [StringLength(10)]
    public string TokenVersion { get; set; } = "1.0";

    // Computed properties
    /// <summary>
    /// Whether the token is currently valid (active and not expired)
    /// </summary>
    public bool IsValid => Status == ClientTokenStatus.Active && 
                          ExpiresAtUtc > DateTime.UtcNow && 
                          !IsDeleted;

    /// <summary>
    /// Whether the token has expired
    /// </summary>
    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;

    /// <summary>
    /// Days until token expires (0 if already expired)
    /// </summary>
    public int DaysUntilExpiry => IsExpired ? 0 : (int)(ExpiresAtUtc - DateTime.UtcNow).TotalDays;

    // Navigation properties
    public Company Company { get; set; } = null!;
    public Subscription Subscription { get; set; } = null!;
    public ICollection<ClientTokenUsageLog> UsageLogs { get; set; } = new List<ClientTokenUsageLog>();
}

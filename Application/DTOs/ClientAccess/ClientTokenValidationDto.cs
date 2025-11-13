using System;
using System.Collections.Generic;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Token validation response for client authentication
/// </summary>
public class ClientTokenValidationDto
{
    /// <summary>
    /// Whether the token is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation timestamp
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Token information (if valid)
    /// </summary>
    public Guid? TokenId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DaysUntilExpiry { get; set; }

    /// <summary>
    /// Company and subscription information (if valid)
    /// </summary>
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string? PlanName { get; set; }

    /// <summary>
    /// Token permissions
    /// </summary>
    public List<string> AllowedEndpoints { get; set; } = new();
    public Dictionary<string, bool> Permissions { get; set; } = new();

    /// <summary>
    /// Rate limiting information
    /// </summary>
    public int? RateLimitPerHour { get; set; }
    public int? RemainingRequestsThisHour { get; set; }
    public DateTime? RateLimitResetTime { get; set; }

    /// <summary>
    /// Validation result details
    /// </summary>
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Security information
    /// </summary>
    public bool RequiresRenewal { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }

    /// <summary>
    /// Subscription health
    /// </summary>
    public bool? SubscriptionActive { get; set; }
    public bool? SubscriptionExpired { get; set; }
    public string? SubscriptionStatus { get; set; }
}

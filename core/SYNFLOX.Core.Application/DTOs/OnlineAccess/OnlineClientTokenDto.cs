using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs.OnlineAccess;

/// <summary>
/// DTO for OnlineClientToken display.
/// </summary>
public class OnlineClientTokenDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SubscriptionId { get; set; }
    public required string Name { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClientTokenStatus Status { get; set; }
    public string? RevocationReason { get; set; }
    public bool AutoRefreshEnabled { get; set; }
    public int? MaxDevices { get; set; }
    public int BoundDeviceCount { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public int UsageCount { get; set; }
    public string? LastUsedFromIp { get; set; }
    public string? Notes { get; set; }
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public int DaysUntilExpiry { get; set; }
    
    // Related info
    public string? CompanyName { get; set; }
    public string? SubscriptionPlanName { get; set; }
}

/// <summary>
/// Request DTO for generating a new online client token.
/// </summary>
public class GenerateOnlineTokenRequest
{
    /// <summary>
    /// The subscription to generate a token for (encrypted).
    /// </summary>
    [Required]
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Friendly name for the token.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }
    
    /// <summary>
    /// Days until token expires (optional, uses default if not set).
    /// </summary>
    [Range(1, 365)]
    public int? ExpiryDays { get; set; }
    
    /// <summary>
    /// Whether the token should auto-refresh before expiry.
    /// </summary>
    public bool AutoRefreshEnabled { get; set; } = true;
    
    /// <summary>
    /// Maximum devices allowed for this token (optional, uses subscription default).
    /// </summary>
    [Range(0, 100)]
    public int? MaxDevices { get; set; }
    
    /// <summary>
    /// Optional notes about this token.
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO for token generation.
/// </summary>
public class GenerateOnlineTokenResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }  // Only returned once!
    public OnlineClientTokenDto? TokenInfo { get; set; }
}

/// <summary>
/// DTO for token validation result.
/// </summary>
public class OnlineTokenValidationDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public Guid? TokenId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string? CompanyName { get; set; }
    public string? PlanName { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

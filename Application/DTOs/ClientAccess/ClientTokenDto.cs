using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// DTO for client access token information
/// </summary>
public class ClientTokenDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public ClientTokenStatus Status { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public int UsageCount { get; set; }
    public string? LastUsedFromIp { get; set; }
    public List<string> AllowedEndpoints { get; set; } = new();
    public string TokenVersion { get; set; } = string.Empty;
    
    // Computed properties
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public int DaysUntilExpiry { get; set; }
    
    // Security: Never expose the actual token or hash in DTOs
}

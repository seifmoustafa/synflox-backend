using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Simplified request for granting full project access
/// </summary>
public class GrantProjectAccessRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;
    
    public DateTime? ExpiresAt { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

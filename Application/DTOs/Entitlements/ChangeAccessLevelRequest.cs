using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request for changing entitlement access level only
/// </summary>
public class ChangeAccessLevelRequest
{
    [Required]
    public Guid EntitlementId { get; set; }
    
    [Required]
    public EntitlementAccessLevel AccessLevel { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

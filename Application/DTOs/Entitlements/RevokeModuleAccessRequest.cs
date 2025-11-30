using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request for revoking module access
/// </summary>
public class RevokeModuleAccessRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    [Required]
    public Guid ModuleId { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

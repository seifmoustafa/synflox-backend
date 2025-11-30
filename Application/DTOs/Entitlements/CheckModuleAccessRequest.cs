using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to check module access
/// </summary>
public class CheckModuleAccessRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Optional project ID if module is within a project
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    [Required]
    public Guid ModuleId { get; set; }
    
    /// <summary>
    /// Optional operation to check
    /// </summary>
    public string? Operation { get; set; }
    
    /// <summary>
    /// Optional specific feature to check
    /// </summary>
    public string? Feature { get; set; }
}

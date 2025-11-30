using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request DTO for checking access to a resource
/// </summary>
public class AccessCheckRequest
{
    /// <summary>
    /// Subscription ID to check access for
    /// </summary>
    [Required]
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Project ID to check (null for standalone module check)
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    /// <summary>
    /// Module ID to check (null for project-level check)
    /// </summary>
    public Guid? ModuleId { get; set; }
    
    /// <summary>
    /// Specific feature to check (null for general access)
    /// </summary>
    public string? Feature { get; set; }
    
    /// <summary>
    /// Operation to check (GET, POST, PUT, DELETE, EXPORT)
    /// </summary>
    public string? Operation { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to check project access
/// </summary>
public class CheckProjectAccessRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    /// <summary>
    /// Optional operation to check (e.g., "Create", "Read", "Update", "Delete", "Export")
    /// </summary>
    public string? Operation { get; set; }
}

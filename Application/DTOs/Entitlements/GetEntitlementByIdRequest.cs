using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Request to get a single entitlement by ID
/// </summary>
public class GetEntitlementByIdRequest
{
    [Required]
    public Guid EntitlementId { get; set; }
}

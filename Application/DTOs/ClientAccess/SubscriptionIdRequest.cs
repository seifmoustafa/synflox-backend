using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Request containing a subscription ID for decryption
/// </summary>
public class SubscriptionIdRequest
{
    [Required]
    public Guid SubscriptionId { get; set; }
}

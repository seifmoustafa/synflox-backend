using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Request to generate a new client access token
/// </summary>
public class GenerateClientTokenRequest
{
    /// <summary>
    /// The subscription to generate token for
    /// </summary>
    [Required]
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Custom expiry date (optional - defaults to subscription expiry)
    /// </summary>
    public DateTime? CustomExpiryDate { get; set; }

    /// <summary>
    /// Specific endpoints this token can access (optional - defaults to all)
    /// </summary>
    public List<ClientEndpointType>? AllowedEndpoints { get; set; }

    /// <summary>
    /// Reason for generating the token
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Whether to revoke existing tokens for this subscription
    /// </summary>
    public bool RevokeExistingTokens { get; set; } = false;
}

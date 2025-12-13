using System;
using Domain.Enums;

namespace Domain.Abstractions;

using AccessMode = Domain.Enums.SubscriptionAccessMode;

/// <summary>
/// Base context interface for client license validation.
/// Used by both Online and Offline systems to represent the authenticated client.
/// </summary>
public interface IClientLicenseContext
{
    /// <summary>
    /// The company ID.
    /// </summary>
    Guid CompanyId { get; }
    
    /// <summary>
    /// The subscription ID.
    /// </summary>
    Guid SubscriptionId { get; }
    
    /// <summary>
    /// The plan ID for this subscription.
    /// </summary>
    Guid PlanId { get; }
    
    /// <summary>
    /// The token/license ID used for authentication.
    /// </summary>
    Guid TokenId { get; }
    
    /// <summary>
    /// Company name for display purposes.
    /// </summary>
    string CompanyName { get; }
    
    /// <summary>
    /// Plan name for display purposes.
    /// </summary>
    string PlanName { get; }
    
    /// <summary>
    /// Type of client system (Online or Offline).
    /// </summary>
    ClientSystemType SystemType { get; }
    
    /// <summary>
    /// Whether the license/token is currently valid.
    /// </summary>
    bool IsValid { get; }
    
    /// <summary>
    /// Expiry date of the subscription.
    /// </summary>
    DateTime ExpiryDateUtc { get; }
    
    /// <summary>
    /// Current access mode.
    /// </summary>
    AccessMode AccessMode { get; }
}

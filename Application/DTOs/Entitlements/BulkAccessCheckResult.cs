using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Result for bulk access check
/// </summary>
public record BulkAccessCheckResult
{
    /// <summary>
    /// Subscription ID checked
    /// </summary>
    public Guid SubscriptionId { get; init; }
    
    /// <summary>
    /// Current subscription access mode
    /// </summary>
    public SubscriptionAccessMode AccessMode { get; init; }
    
    /// <summary>
    /// Results for each check item
    /// </summary>
    public List<AccessCheckResultItem> Results { get; init; } = new();
}

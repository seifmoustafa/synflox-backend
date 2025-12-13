using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.ValueObjects;

namespace Domain.Abstractions;

/// <summary>
/// Base interface for fetching entitlements.
/// Shared by both Online (API fetch) and Offline (embedded in key) systems.
/// </summary>
public interface IEntitlementProvider
{
    /// <summary>
    /// Get the full entitlement matrix for a subscription.
    /// </summary>
    Task<EntitlementMatrix> GetEntitlementsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the current entitlement version for cache validation.
    /// </summary>
    Task<int> GetEntitlementVersionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a subscription has access to a specific project.
    /// </summary>
    Task<bool> HasProjectAccessAsync(
        Guid subscriptionId,
        Guid projectId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a subscription has access to a specific module.
    /// </summary>
    Task<bool> HasModuleAccessAsync(
        Guid subscriptionId,
        Guid moduleId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a subscription can perform a specific action on a project.
    /// </summary>
    Task<bool> CanPerformActionAsync(
        Guid subscriptionId,
        Guid projectId,
        EntitlementAction action,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Actions that can be performed on entitlements.
/// </summary>
public enum EntitlementAction
{
    Create,
    Read,
    Update,
    Delete,
    Export
}

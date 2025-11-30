using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for managing subscription entitlements
/// </summary>
public interface ISubscriptionEntitlementRepository : IBaseRepository<Guid, SubscriptionEntitlement>
{
    /// <summary>
    /// Gets all active entitlements for a subscription
    /// </summary>
    Task<IEnumerable<SubscriptionEntitlement>> GetBySubscriptionIdAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entitlements for a subscription with related project and module data
    /// </summary>
    Task<IEnumerable<SubscriptionEntitlement>> GetBySubscriptionWithDetailsAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entitlement by subscription and project
    /// </summary>
    Task<SubscriptionEntitlement?> GetBySubscriptionAndProjectAsync(
        Guid subscriptionId, 
        Guid projectId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entitlement by subscription, project, and module
    /// </summary>
    Task<SubscriptionEntitlement?> GetBySubscriptionProjectAndModuleAsync(
        Guid subscriptionId, 
        Guid projectId, 
        Guid moduleId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entitlement by subscription and standalone module (no project)
    /// </summary>
    Task<SubscriptionEntitlement?> GetBySubscriptionAndModuleAsync(
        Guid subscriptionId, 
        Guid moduleId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if subscription has access to a specific project
    /// </summary>
    Task<bool> HasProjectAccessAsync(
        Guid subscriptionId, 
        Guid projectId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if subscription has access to a specific module within a project
    /// </summary>
    Task<bool> HasModuleAccessAsync(
        Guid subscriptionId, 
        Guid projectId, 
        Guid moduleId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if subscription has access to a standalone module
    /// </summary>
    Task<bool> HasStandaloneModuleAccessAsync(
        Guid subscriptionId, 
        Guid moduleId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entitlements by source (e.g., all admin-granted entitlements)
    /// </summary>
    Task<IEnumerable<SubscriptionEntitlement>> GetBySourceAsync(
        Guid subscriptionId, 
        EntitlementSource source, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all custom (non-plan-default) entitlements for a subscription
    /// </summary>
    Task<IEnumerable<SubscriptionEntitlement>> GetCustomEntitlementsAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entitlements that are expiring within the specified days
    /// </summary>
    Task<IEnumerable<SubscriptionEntitlement>> GetExpiringEntitlementsAsync(
        int withinDays, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all entitlements for a subscription (soft delete)
    /// </summary>
    Task<int> DeactivateAllForSubscriptionAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts entitlements for a subscription
    /// </summary>
    Task BulkInsertAsync(
        IEnumerable<SubscriptionEntitlement> entitlements, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entitlements count by subscription
    /// </summary>
    Task<int> GetCountBySubscriptionAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
}

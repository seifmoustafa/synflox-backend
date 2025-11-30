using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Entitlements;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Service interface for managing subscription entitlements
/// </summary>
public interface IEntitlementService
{
    #region CRUD Operations
    
    /// <summary>
    /// Get all entitlements for a subscription
    /// </summary>
    Task<IEnumerable<SubscriptionEntitlementDto>> GetSubscriptionEntitlementsAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a specific entitlement by ID
    /// </summary>
    Task<SubscriptionEntitlementDto?> GetEntitlementByIdAsync(
        Guid entitlementId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Grant a new entitlement to a subscription
    /// </summary>
    Task<SubscriptionEntitlementDto> GrantEntitlementAsync(
        GrantEntitlementRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an existing entitlement
    /// </summary>
    Task<SubscriptionEntitlementDto> UpdateEntitlementAsync(
        UpdateEntitlementRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revoke an entitlement
    /// </summary>
    Task RevokeEntitlementAsync(
        RevokeEntitlementRequest request, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Simplified Grant Operations
    
    /// <summary>
    /// Grant full access to a project
    /// </summary>
    Task<SubscriptionEntitlementDto> GrantProjectAccessAsync(
        GrantProjectAccessRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Grant access to a specific module within a project
    /// </summary>
    Task<SubscriptionEntitlementDto> GrantModuleAccessAsync(
        GrantModuleAccessRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Grant access to a standalone module
    /// </summary>
    Task<SubscriptionEntitlementDto> GrantStandaloneModuleAccessAsync(
        GrantStandaloneModuleRequest request, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Bulk Operations
    
    /// <summary>
    /// Copy all plan entitlements to a subscription (on subscription creation)
    /// </summary>
    Task<int> CopyPlanEntitlementsToSubscriptionAsync(
        Guid subscriptionId, 
        Guid planId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add upgrade entitlements (additive - keeps existing custom grants)
    /// </summary>
    Task<int> AddUpgradeEntitlementsAsync(
        Guid subscriptionId, 
        Guid newPlanId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Replace all entitlements with a new plan's entitlements
    /// </summary>
    Task<int> ReplaceEntitlementsAsync(
        Guid subscriptionId, 
        Guid newPlanId, 
        bool keepCustomGrants = true,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Downgrade subscription to fallback plan entitlements
    /// </summary>
    Task<int> DowngradeToFallbackAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revoke all entitlements for a subscription
    /// </summary>
    Task<int> RevokeAllEntitlementsAsync(
        RevokeAllEntitlementsRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bulk revoke specific entitlements
    /// </summary>
    Task<int> BulkRevokeAsync(
        BulkRevokeRequest request, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Access Checking
    
    /// <summary>
    /// Check access to a specific resource
    /// </summary>
    Task<AccessCheckResult> CheckAccessAsync(
        AccessCheckRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if subscription has access to a project
    /// </summary>
    Task<bool> HasProjectAccessAsync(
        Guid subscriptionId, 
        Guid projectId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if subscription has access to a module
    /// </summary>
    Task<bool> HasModuleAccessAsync(
        Guid subscriptionId, 
        Guid projectId, 
        Guid moduleId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if subscription has access to a standalone module
    /// </summary>
    Task<bool> HasStandaloneModuleAccessAsync(
        Guid subscriptionId, 
        Guid moduleId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if operation is allowed on a resource
    /// </summary>
    Task<bool> IsOperationAllowedAsync(
        Guid subscriptionId, 
        Guid? projectId, 
        Guid? moduleId, 
        string operation, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check multiple resources at once
    /// </summary>
    Task<BulkAccessCheckResult> BulkCheckAccessAsync(
        BulkAccessCheckRequest request, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Entitlement Matrix
    
    /// <summary>
    /// Build complete entitlement matrix for a subscription
    /// </summary>
    Task<EntitlementMatrixDto> BuildEntitlementMatrixAsync(
        Guid subscriptionId, 
        bool includeUpgrades = true,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get entitlement matrix with caching
    /// </summary>
    Task<EntitlementMatrixDto> GetCachedEntitlementMatrixAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Versioning
    
    /// <summary>
    /// Get current entitlements version for a subscription
    /// </summary>
    Task<int> GetEntitlementsVersionAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increment entitlements version (called automatically on any change)
    /// </summary>
    Task<int> IncrementVersionAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidate cached entitlement matrix
    /// </summary>
    Task InvalidateCacheAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Access Mode Management
    
    /// <summary>
    /// Get effective access mode for a subscription
    /// </summary>
    Task<SubscriptionAccessMode> GetEffectiveAccessModeAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update subscription access mode
    /// </summary>
    Task UpdateAccessModeAsync(
        Guid subscriptionId, 
        SubscriptionAccessMode newMode, 
        string? restrictionMessage = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if subscription is in grace period
    /// </summary>
    Task<bool> IsInGracePeriodAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get days remaining in current access mode
    /// </summary>
    Task<int?> GetDaysRemainingInCurrentModeAsync(
        Guid subscriptionId, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Utilities
    
    /// <summary>
    /// Set CRUD flags based on access level
    /// </summary>
    (bool CanCreate, bool CanRead, bool CanUpdate, bool CanDelete, bool CanExport) GetCrudFlagsForAccessLevel(
        EntitlementAccessLevel level);
    
    /// <summary>
    /// Clone entitlement for a new subscription
    /// </summary>
    Task<SubscriptionEntitlementDto> CloneEntitlementAsync(
        Guid sourceEntitlementId, 
        Guid targetSubscriptionId, 
        EntitlementSource source,
        CancellationToken cancellationToken = default);
    
    #endregion
}

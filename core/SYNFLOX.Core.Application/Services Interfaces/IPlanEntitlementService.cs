using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.PlanEntitlements;

namespace Application.Services;

/// <summary>
/// Service interface for plan-level entitlement operations
/// </summary>
public interface IPlanEntitlementService
{
    /// <summary>
    /// Get all entitlements for a plan
    /// </summary>
    Task<IEnumerable<PlanEntitlementDto>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a single entitlement by ID
    /// </summary>
    Task<PlanEntitlementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a new plan entitlement
    /// </summary>
    Task<PlanEntitlementDto> CreateAsync(CreatePlanEntitlementRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an existing plan entitlement.
    /// Returns response with conflict info if project has child overrides.
    /// </summary>
    Task<UpdatePlanEntitlementResponse> UpdateAsync(UpdatePlanEntitlementRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a plan entitlement (soft delete)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete all entitlements for a plan
    /// </summary>
    Task DeleteAllByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Copy entitlements from one plan to another
    /// </summary>
    Task CopyEntitlementsAsync(Guid sourcePlanId, Guid targetPlanId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get count of entitlements for a plan
    /// </summary>
    Task<int> GetCountByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Grant full access to a project (convenience method)
    /// </summary>
    Task<PlanEntitlementDto> GrantProjectAccessAsync(Guid planId, Guid projectId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Grant full access to a module (convenience method)
    /// </summary>
    Task<PlanEntitlementDto> GrantModuleAccessAsync(Guid planId, Guid moduleId, CancellationToken cancellationToken = default);
}

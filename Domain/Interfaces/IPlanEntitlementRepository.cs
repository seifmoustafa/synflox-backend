using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for PlanEntitlement operations
/// </summary>
public interface IPlanEntitlementRepository : IBaseRepository<Guid, PlanEntitlement>
{
    /// <summary>
    /// Get all entitlements for a specific plan
    /// </summary>
    Task<IEnumerable<PlanEntitlement>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all entitlements for a specific plan with details (Project/Module)
    /// </summary>
    Task<IEnumerable<PlanEntitlement>> GetByPlanIdWithDetailsAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get entitlement by plan, project, and module combination
    /// </summary>
    Task<PlanEntitlement?> GetByPlanAndTargetAsync(Guid planId, Guid? projectId, Guid? moduleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if an entitlement already exists for the given target
    /// </summary>
    Task<bool> ExistsAsync(Guid planId, Guid? projectId, Guid? moduleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get count of entitlements for a plan
    /// </summary>
    Task<int> GetCountByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bulk delete all entitlements for a plan
    /// </summary>
    Task DeleteAllByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Copy entitlements from one plan to another
    /// </summary>
    Task CopyEntitlementsAsync(Guid sourcePlanId, Guid targetPlanId, CancellationToken cancellationToken = default);
}

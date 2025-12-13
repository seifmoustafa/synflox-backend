using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Domain.Interfaces;

public interface ISubscriptionPlanRepository : IBaseRepository<Guid, SubscriptionPlan>
{
    Task<SubscriptionPlan?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SubscriptionPlan>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<decimal?> GetPriceAsync(Guid planId, Currency currency, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get plan with projects and modules for entitlement copying
    /// </summary>
    Task<SubscriptionPlan?> GetWithProjectsAndModulesAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all free tier plans (for fallback plan selection)
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> GetFreeTierPlansAsync(CancellationToken cancellationToken = default);
    
    // Delete cascade support
    Task<int> GetChildPlansCountAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<int> GetFallbackReferencesCountAsync(Guid planId, CancellationToken cancellationToken = default);
    Task NullifyChildPlanReferencesAsync(Guid planId, CancellationToken cancellationToken = default);
    Task NullifyFallbackReferencesAsync(Guid planId, CancellationToken cancellationToken = default);
}

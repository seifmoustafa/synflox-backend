using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.OnlineAccess;
using Domain.Enums;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for SubscriptionChangeLog entity operations.
/// </summary>
public interface ISubscriptionChangeLogRepository : IBaseRepository<Guid, SubscriptionChangeLog>
{
    /// <summary>
    /// Get pending changes for a subscription.
    /// </summary>
    Task<IEnumerable<SubscriptionChangeLog>> GetPendingChangesAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get pending changes for a plan (affects all subscriptions).
    /// </summary>
    Task<IEnumerable<SubscriptionChangeLog>> GetPendingPlanChangesAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all changes ready to be applied (effective date has passed).
    /// </summary>
    Task<IEnumerable<SubscriptionChangeLog>> GetChangesReadyToApplyAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all changes ready to be applied as of a specific date.
    /// </summary>
    Task<IEnumerable<SubscriptionChangeLog>> GetChangesReadyToApplyAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get changes by type for a subscription.
    /// </summary>
    Task<IEnumerable<SubscriptionChangeLog>> GetByChangeTypeAsync(Guid subscriptionId, string changeType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get applied changes for a subscription within a date range.
    /// </summary>
    Task<IEnumerable<SubscriptionChangeLog>> GetAppliedChangesAsync(Guid subscriptionId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark a change as applied.
    /// </summary>
    Task MarkAsAppliedAsync(Guid changeId, string appliedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a pending change.
    /// </summary>
    Task CancelChangeAsync(Guid changeId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get changes that need customer notification.
    /// </summary>
    Task<IEnumerable<SubscriptionChangeLog>> GetUnnotifiedChangesAsync(CancellationToken cancellationToken = default);
}

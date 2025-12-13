using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for AccessTimeWindow entity.
/// Provides operations for managing plan access time windows.
/// </summary>
public interface IAccessTimeWindowRepository : IBaseRepository<Guid, AccessTimeWindow>
{
    /// <summary>
    /// Get all time windows for a plan.
    /// </summary>
    Task<List<AccessTimeWindow>> GetByPlanIdAsync(Guid planId, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active time windows for a plan.
    /// </summary>
    Task<List<AccessTimeWindow>> GetActiveByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if current time is within any active window for a plan.
    /// </summary>
    Task<bool> IsWithinActiveWindowAsync(Guid planId, string? companyTimezoneId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all time windows for a plan.
    /// </summary>
    Task DeleteByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk create time windows for a plan.
    /// </summary>
    Task BulkCreateAsync(Guid planId, IEnumerable<AccessTimeWindow> windows, CancellationToken cancellationToken = default);
}

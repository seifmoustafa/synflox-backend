using Domain.Entities.Subscriptions;

namespace Domain.Interfaces.Repositories;

public interface ISubscriptionHistoryRepository : IBaseRepository<Guid, SubscriptionHistory>
{
    /// <summary>
    /// Get history for a specific subscription
    /// </summary>
    Task<IEnumerable<SubscriptionHistory>> GetBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Add a history entry
    /// </summary>
    Task AddHistoryEntryAsync(SubscriptionHistory historyEntry);
}

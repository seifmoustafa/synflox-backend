using Domain.Entities.Subscriptions;
using Domain.Interfaces.Repositories;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SubscriptionHistoryRepository : BaseRepository<Guid, SubscriptionHistory>, ISubscriptionHistoryRepository
{
    public SubscriptionHistoryRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SubscriptionHistory>> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        // Don't filter by IsDeleted for history entries - they should always be visible
        return await _context.Set<SubscriptionHistory>()
            .Where(h => h.SubscriptionId == subscriptionId)
            .OrderByDescending(h => h.CreatedTimestamp)
            .ToListAsync();
    }

    public async Task AddHistoryEntryAsync(SubscriptionHistory historyEntry)
    {
        await _context.Set<SubscriptionHistory>().AddAsync(historyEntry);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OutboxEventRepository : IOutboxEventRepository
{
    private readonly ApplicationDBContext _context;

    public OutboxEventRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<OutboxEvent> AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        await _context.Set<OutboxEvent>().AddAsync(outboxEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return outboxEvent;
    }

    public async Task<IEnumerable<OutboxEvent>> GetUnprocessedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        return await _context.Set<OutboxEvent>()
            .Where(e => !e.IsProcessed && e.AttemptCount < maxAttempts)
            .OrderBy(e => e.CreatedAtUtc)
            .Take(100) // Process in batches
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var outboxEvent = await _context.Set<OutboxEvent>().FindAsync(new object[] { eventId }, cancellationToken);
        if (outboxEvent != null)
        {
            outboxEvent.IsProcessed = true;
            outboxEvent.ProcessedAtUtc = DateTime.UtcNow;
            outboxEvent.ErrorMessage = null;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid eventId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var outboxEvent = await _context.Set<OutboxEvent>().FindAsync(new object[] { eventId }, cancellationToken);
        if (outboxEvent != null)
        {
            outboxEvent.AttemptCount++;
            outboxEvent.ErrorMessage = errorMessage.Length > 2000 ? errorMessage.Substring(0, 2000) : errorMessage;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<OutboxEvent>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<OutboxEvent>()
            .Where(e => e.SubscriptionId == subscriptionId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

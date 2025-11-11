using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;

namespace Domain.Interfaces;

public interface IOutboxEventRepository
{
    Task<OutboxEvent> AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxEvent>> GetUnprocessedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid eventId, string errorMessage, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxEvent>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}

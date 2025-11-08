using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Webhooks;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for WebhookDelivery entity operations.
/// </summary>
public interface IWebhookDeliveryRepository : IBaseRepository<Guid, WebhookDelivery>
{
    /// <summary>
    /// Gets delivery history for a webhook.
    /// </summary>
    Task<IEnumerable<WebhookDelivery>> GetByWebhookIdAsync(Guid webhookId, int skip = 0, int take = 10);

    /// <summary>
    /// Gets failed deliveries that need retry.
    /// </summary>
    Task<IEnumerable<WebhookDelivery>> GetFailedDeliveriesForRetryAsync(int maxAttempts = 3);

    /// <summary>
    /// Counts deliveries for a webhook.
    /// </summary>
    Task<int> CountByWebhookIdAsync(Guid webhookId);
}


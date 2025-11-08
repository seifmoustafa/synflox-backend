using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Webhooks;
using Domain.Enums;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for Webhook entity operations.
/// </summary>
public interface IWebhookRepository : IBaseRepository<Guid, Webhook>
{
    /// <summary>
    /// Gets all active webhooks for a company that subscribe to a specific event type.
    /// </summary>
    Task<IEnumerable<Webhook>> GetActiveWebhooksByCompanyAndEventAsync(
        Guid companyId,
        WebhookEventType eventType);

    /// <summary>
    /// Gets all webhooks for a company.
    /// </summary>
    Task<IEnumerable<Webhook>> GetByCompanyIdAsync(Guid companyId, int skip = 0, int take = 10);

    /// <summary>
    /// Counts webhooks for a company.
    /// </summary>
    Task<int> CountByCompanyIdAsync(Guid companyId);
}


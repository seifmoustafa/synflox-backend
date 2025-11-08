using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Webhooks;
using Domain.Entities.Common;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Service interface for managing webhooks.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    Task<WebhookDto> CreateWebhookAsync(CreateWebhookRequest request);

    /// <summary>
    /// Gets all webhooks for a company.
    /// </summary>
    Task<(IEnumerable<WebhookDto> Webhooks, PaginationMetadata Meta)> GetWebhooksByCompanyAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets all webhooks.
    /// </summary>
    Task<(IEnumerable<WebhookDto> Webhooks, PaginationMetadata Meta)> GetAllWebhooksAsync(
        Guid? companyId = null,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    Task DeleteWebhookAsync(Guid webhookId);

    /// <summary>
    /// Triggers a webhook event for all subscribed webhooks.
    /// </summary>
    Task TriggerWebhookAsync(Guid companyId, WebhookEventType eventType, object payload);

    /// <summary>
    /// Retries failed webhook deliveries.
    /// </summary>
    Task RetryFailedWebhooksAsync(int maxRetries = 3);

    /// <summary>
    /// Gets delivery history for a webhook.
    /// </summary>
    Task<(IEnumerable<WebhookDeliveryDto> Deliveries, PaginationMetadata Meta)> GetDeliveryHistoryAsync(
        Guid webhookId,
        int page = 1,
        int pageSize = 10);
}


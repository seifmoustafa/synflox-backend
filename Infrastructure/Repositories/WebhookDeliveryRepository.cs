using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Webhooks;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WebhookDeliveryRepository : BaseRepository<Guid, WebhookDelivery>, IWebhookDeliveryRepository
{
    public WebhookDeliveryRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WebhookDelivery>> GetByWebhookIdAsync(Guid webhookId, int skip = 0, int take = 10)
    {
        return await _dbSet
            .Where(d => d.WebhookId == webhookId && !d.IsDeleted)
            .OrderByDescending(d => d.AttemptedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<WebhookDelivery>> GetFailedDeliveriesForRetryAsync(int maxAttempts = 3)
    {
        return await _dbSet
            .Where(d => !d.Succeeded &&
                       d.AttemptNumber < maxAttempts &&
                       !d.IsDeleted)
            .OrderBy(d => d.AttemptedAt)
            .ToListAsync();
    }

    public async Task<int> CountByWebhookIdAsync(Guid webhookId)
    {
        return await _dbSet
            .CountAsync(d => d.WebhookId == webhookId && !d.IsDeleted);
    }
}


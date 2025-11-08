using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Entities.Webhooks;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WebhookRepository : BaseRepository<Guid, Webhook>, IWebhookRepository
{
    public WebhookRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Webhook>> GetActiveWebhooksByCompanyAndEventAsync(
        Guid companyId,
        WebhookEventType eventType)
    {
        var allWebhooks = await _dbSet
            .Where(w => w.CompanyId == companyId && w.IsActive && !w.IsDeleted)
            .ToListAsync();

        // Filter webhooks that subscribe to this event type
        var subscribedWebhooks = new List<Webhook>();
        foreach (var webhook in allWebhooks)
        {
            try
            {
                var events = JsonSerializer.Deserialize<WebhookEventType[]>(webhook.Events);
                if (events != null && events.Contains(eventType))
                {
                    subscribedWebhooks.Add(webhook);
                }
            }
            catch
            {
                // Skip invalid JSON
            }
        }

        return subscribedWebhooks;
    }

    public async Task<IEnumerable<Webhook>> GetByCompanyIdAsync(Guid companyId, int skip = 0, int take = 10)
    {
        return await _dbSet
            .Where(w => w.CompanyId == companyId && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedTimestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId)
    {
        return await _dbSet
            .CountAsync(w => w.CompanyId == companyId && !w.IsDeleted);
    }
}


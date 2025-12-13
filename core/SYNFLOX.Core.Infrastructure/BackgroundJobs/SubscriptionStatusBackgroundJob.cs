using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that processes subscription status changes
/// Runs every 10 minutes to check for:
/// - Expired subscriptions (past expiry + grace)
/// - Deferred upgrades due for activation
/// - Auto-renewals
/// </summary>
public class SubscriptionStatusBackgroundJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionStatusBackgroundJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    public SubscriptionStatusBackgroundJob(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionStatusBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Status Background Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSubscriptionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscriptions in background job");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Subscription Status Background Job stopped");
    }

    private async Task ProcessSubscriptionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var planRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionPlanRepository>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;

        // 1. Process expired subscriptions
        await ProcessExpiredSubscriptionsAsync(subscriptionRepo, outboxRepo, unitOfWork, now);

        // 2. Process deferred upgrades
        await ProcessDeferredUpgradesAsync(subscriptionRepo, planRepo, outboxRepo, unitOfWork, now);

        // 3. Process auto-renewals
        await ProcessAutoRenewalsAsync(subscriptionRepo, planRepo, outboxRepo, unitOfWork, now);
    }

    private async Task ProcessExpiredSubscriptionsAsync(
        ISubscriptionRepository subscriptionRepo,
        IOutboxEventRepository outboxRepo,
        IUnitOfWork unitOfWork,
        DateTime now)
    {
        var expiredSubs = await subscriptionRepo.GetExpiredSubscriptionsAsync(now);
        var count = 0;

        foreach (var subscription in expiredSubs)
        {
            // Skip lifetime subscriptions - they never expire naturally
            if (subscription.IsLifetime)
            {
                _logger.LogDebug("Skipping lifetime subscription {SubscriptionId} from expiry processing", subscription.Id);
                continue;
            }

            subscription.IsExpired = true;
            subscription.IsActive = false;
            subscription.StatusReason = "Expired";

            await subscriptionRepo.UpdateAsync(subscription);

            // Create outbox event
            await outboxRepo.AddAsync(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = SubscriptionEventType.Expired,
                CompanyId = subscription.CompanyId,
                SubscriptionId = subscription.Id,
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    CompanyName = subscription.Company.Name,
                    CompanyEmail = subscription.Company.ContactEmail,
                    PlanName = subscription.Plan.Name
                }),
                CreatedAtUtc = now,
                IsProcessed = false,
                AttemptCount = 0
            });

            count++;
        }

        if (count > 0)
        {
            await unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Marked {count} subscriptions as expired");
        }
    }

    private async Task ProcessDeferredUpgradesAsync(
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        IOutboxEventRepository outboxRepo,
        IUnitOfWork unitOfWork,
        DateTime now)
    {
        var subsForActivation = await subscriptionRepo.GetSubscriptionsDueForActivationAsync(now);
        var count = 0;

        foreach (var currentSubscription in subsForActivation)
        {
            // With new architecture, NextSubscriptionId points to an already-created subscription
            // We just need to activate it
            if (!currentSubscription.NextSubscriptionId.HasValue) continue;
            
            var nextSubscription = await subscriptionRepo.GetWithDetailsAsync(currentSubscription.NextSubscriptionId.Value);
            if (nextSubscription == null) continue;
            
            // Activate the scheduled subscription
            nextSubscription.IsActive = true;
            nextSubscription.StartDateUtc = now;
            nextSubscription.ExpiryDateUtc = PlanDurationHelper.CalculateExpiryDate(now, nextSubscription.Plan.DurationType);
            nextSubscription.AccessMode = SubscriptionAccessMode.Full;
            nextSubscription.StatusReason = $"Deferred upgrade activated from {currentSubscription.Plan.Name}";
            await subscriptionRepo.UpdateAsync(nextSubscription);

            // Deactivate the current subscription
            currentSubscription.IsActive = false;
            currentSubscription.NextSubscriptionId = null;
            currentSubscription.NextSubscriptionActivationDateUtc = null;
            currentSubscription.StatusReason = $"Upgraded to {nextSubscription.Plan.Name} (Deferred)";
            await subscriptionRepo.UpdateAsync(currentSubscription);

            // Create outbox event
            await outboxRepo.AddAsync(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = SubscriptionEventType.DeferredActivated,
                CompanyId = nextSubscription.CompanyId,
                SubscriptionId = nextSubscription.Id,
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    CompanyName = currentSubscription.Company.Name,
                    CompanyEmail = currentSubscription.Company.ContactEmail,
                    OldPlanName = currentSubscription.Plan.Name,
                    NewPlanName = nextSubscription.Plan.Name,
                    NewExpiryDate = nextSubscription.ExpiryDateUtc
                }),
                CreatedAtUtc = now,
                IsProcessed = false,
                AttemptCount = 0
            });

            count++;
        }

        if (count > 0)
        {
            await unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Activated {count} deferred upgrades");
        }
    }

    private async Task ProcessAutoRenewalsAsync(
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        IOutboxEventRepository outboxRepo,
        IUnitOfWork unitOfWork,
        DateTime now)
    {
        var subsForRenewal = await subscriptionRepo.GetSubscriptionsForAutoRenewalAsync(now);
        var count = 0;

        foreach (var subscription in subsForRenewal)
        {
            // Skip lifetime subscriptions - they cannot auto-renew (already permanent)
            if (subscription.IsLifetime)
            {
                _logger.LogDebug("Skipping lifetime subscription {SubscriptionId} from auto-renewal", subscription.Id);
                continue;
            }

            // Check for duplicate (idempotency)
            var existing = await subscriptionRepo.FindAsync(s => 
                s.ParentSubscriptionId == subscription.Id && 
                s.StartDateUtc >= subscription.ExpiryDateUtc);
            
            if (existing.Any()) continue;

            var plan = subscription.Plan;
            var price = await planRepo.GetPriceAsync(subscription.PlanId, subscription.Currency);
            if (!price.HasValue) continue;

            // Create renewal subscription using PlanDurationHelper
            var startDate = subscription.ExpiryDateUtc.AddSeconds(1);
            var newSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                CompanyId = subscription.CompanyId,
                PlanId = subscription.PlanId,
                StartDateUtc = startDate,
                ExpiryDateUtc = PlanDurationHelper.CalculateExpiryDate(startDate, plan.DurationType),
                IsActive = true,
                IsExpired = false,
                IsTrial = false,
                AutoRenew = subscription.AutoRenew,
                Currency = subscription.Currency,
                Amount = price.Value,
                ParentSubscriptionId = subscription.Id,
                StatusReason = "Auto-renewed"
            };

            await subscriptionRepo.AddAsync(newSubscription);

            // Create outbox event
            await outboxRepo.AddAsync(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = SubscriptionEventType.AutoRenewed,
                CompanyId = newSubscription.CompanyId,
                SubscriptionId = newSubscription.Id,
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    CompanyName = subscription.Company.Name,
                    CompanyEmail = subscription.Company.ContactEmail,
                    PlanName = plan.Name,
                    NewExpiryDate = newSubscription.ExpiryDateUtc
                }),
                CreatedAtUtc = now,
                IsProcessed = false,
                AttemptCount = 0
            });

            count++;
        }

        if (count > 0)
        {
            await unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Auto-renewed {count} subscriptions");
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that processes pending subscription changes.
/// 
/// Applies changes that are scheduled for:
/// - Next billing cycle (price changes, feature removals, downgrades)
/// - Immediate effect (feature additions, upgrades - handled inline, not here)
/// 
/// Key behaviors:
/// - Checks SubscriptionChangeLog for pending changes
/// - Applies changes when EffectiveDateUtc is reached
/// - Increments subscription EntitlementsVersion on change
/// - Notifies customers of applied changes
/// 
/// Runs every hour.
/// </summary>
public class SubscriptionChangeProcessorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionChangeProcessorJob> _logger;
    
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public SubscriptionChangeProcessorJob(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionChangeProcessorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Change Processor Job started");

        // Initial delay to allow system to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Subscription Change Processor Job");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Subscription Change Processor Job stopped");
    }

    private async Task ProcessPendingChangesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting subscription change processing");

        using var scope = _serviceProvider.CreateScope();
        var changeLogRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionChangeLogRepository>();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var appliedCount = 0;

        // Get all changes ready to apply (effective date reached, not applied, not cancelled)
        var readyChanges = await changeLogRepo.GetChangesReadyToApplyAsync(now, cancellationToken);
        var changesList = readyChanges.ToList();

        if (!changesList.Any())
        {
            _logger.LogDebug("No pending changes ready to apply");
            return;
        }

        _logger.LogInformation("Found {Count} changes ready to apply", changesList.Count);

        // Group by subscription to process all changes for a subscription together
        var groupedChanges = changesList.GroupBy(c => c.SubscriptionId);

        foreach (var group in groupedChanges)
        {
            try
            {
                var subscription = await subscriptionRepo.GetByIdAsync(group.Key, null, cancellationToken);
                if (subscription == null)
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found, skipping changes", group.Key);
                    continue;
                }

                var changesApplied = false;

                foreach (var change in group.OrderBy(c => c.EffectiveDateUtc))
                {
                    try
                    {
                        await ApplyChangeAsync(change, subscription, changeLogRepo, subscriptionRepo, now, cancellationToken);
                        changesApplied = true;
                        appliedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to apply change {ChangeId} for subscription {SubscriptionId}", 
                            change.Id, change.SubscriptionId);
                    }
                }

                // Increment entitlements version if any changes applied
                if (changesApplied)
                {
                    subscription.EntitlementsVersion++;
                    subscription.UpdatedTimestamp = now;
                    await subscriptionRepo.UpdateAsync(subscription, cancellationToken);

                    // Create notification event
                    await CreateChangeNotificationAsync(outboxRepo, subscription, group.Count(), now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing changes for subscription {SubscriptionId}", group.Key);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Applied {Count} subscription changes", appliedCount);
    }

    private async Task ApplyChangeAsync(
        Domain.Entities.OnlineAccess.SubscriptionChangeLog change,
        Subscription subscription,
        ISubscriptionChangeLogRepository changeLogRepo,
        ISubscriptionRepository subscriptionRepo,
        DateTime now,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying change {ChangeId}: {ChangeType} for subscription {SubscriptionId}",
            change.Id, change.ChangeType, change.SubscriptionId);

        // Apply the change based on type
        switch (change.ChangeType.ToUpperInvariant())
        {
            case "PLAN_CHANGE":
                if (change.PlanId.HasValue)
                {
                    subscription.PlanId = change.PlanId.Value;
                }
                break;

            case "DEVICE_LIMIT_CHANGE":
                // Device limit is handled at plan level, no subscription property to update
                break;

            case "ACCESS_MODE_CHANGE":
                if (!string.IsNullOrEmpty(change.NewValue) && 
                    Enum.TryParse<SubscriptionAccessMode>(change.NewValue, out var newMode))
                {
                    subscription.AccessMode = newMode;
                }
                break;

            case "FEATURE_REMOVAL":
                // Feature changes are handled through entitlements, version increment is enough
                break;

            case "DOWNGRADE":
                if (change.PlanId.HasValue)
                {
                    subscription.PlanId = change.PlanId.Value;
                }
                break;

            default:
                _logger.LogWarning("Unknown change type: {ChangeType}", change.ChangeType);
                break;
        }

        // Mark change as applied
        change.IsApplied = true;
        change.AppliedAtUtc = now;
        change.AppliedBy = "System:ChangeProcessorJob";
        change.UpdatedTimestamp = now;

        await changeLogRepo.UpdateAsync(change, cancellationToken);
    }

    private async Task CreateChangeNotificationAsync(
        IOutboxEventRepository outboxRepo,
        Subscription subscription,
        int changeCount,
        DateTime now)
    {
        await outboxRepo.AddAsync(new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = SubscriptionEventType.StatusChanged,
            CompanyId = subscription.CompanyId,
            SubscriptionId = subscription.Id,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                NotificationType = "ScheduledChangesApplied",
                SubscriptionId = subscription.Id,
                CompanyName = subscription.Company?.Name ?? "Unknown",
                PlanName = subscription.Plan?.Name ?? "Unknown",
                ChangesApplied = changeCount,
                NewEntitlementsVersion = subscription.EntitlementsVersion,
                AppliedAtUtc = now
            }),
            CreatedAtUtc = now,
            IsProcessed = false,
            AttemptCount = 0
        });
    }
}

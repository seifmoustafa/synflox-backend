using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that transitions subscription AccessMode based on time-based rules.
/// Runs every hour to check for:
/// - Full → GracePeriod (when subscription expires but within grace period)
/// - GracePeriod → ExportOnly (when grace period ends, if export deadline set)
/// - GracePeriod → ReadOnly (when grace period ends, if fallback plan exists)
/// - GracePeriod → Blocked (when grace period ends, no fallback)
/// - ExportOnly → Blocked (when export deadline passes)
/// 
/// Each transition increments EntitlementsVersion to invalidate client caches.
/// </summary>
public class AccessModeTransitionJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AccessModeTransitionJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Run every hour

    public AccessModeTransitionJob(
        IServiceProvider serviceProvider,
        ILogger<AccessModeTransitionJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AccessModeTransitionJob started - runs every {Interval}", _interval);

        // Initial delay to let the application fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAccessModeTransitionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AccessModeTransitionJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("AccessModeTransitionJob stopped");
    }

    private async Task ProcessAccessModeTransitionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing access mode transitions...");

        using var scope = _serviceProvider.CreateScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var entitlementService = scope.ServiceProvider.GetRequiredService<IEntitlementService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

        var now = DateTime.UtcNow;
        var transitions = new List<AccessModeTransition>();

        // Get all active subscriptions that need access mode evaluation
        var subscriptions = await subscriptionRepo.GetAllWithPlansAsync(cancellationToken);

        foreach (var subscription in subscriptions.Where(s => !s.IsDeleted))
        {
            var currentMode = subscription.AccessMode;
            var newMode = CalculateAccessMode(subscription, now);

            if (currentMode != newMode)
            {
                transitions.Add(new AccessModeTransition
                {
                    Subscription = subscription,
                    OldMode = currentMode,
                    NewMode = newMode
                });
            }
        }

        if (transitions.Count == 0)
        {
            _logger.LogDebug("No access mode transitions needed");
            return;
        }

        _logger.LogInformation("Processing {Count} access mode transitions", transitions.Count);

        foreach (var transition in transitions)
        {
            try
            {
                await ProcessTransitionAsync(
                    transition,
                    subscriptionRepo,
                    entitlementService,
                    outboxRepo,
                    unitOfWork,
                    now,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transition subscription {SubscriptionId} from {OldMode} to {NewMode}",
                    transition.Subscription.Id, transition.OldMode, transition.NewMode);
            }
        }

        _logger.LogInformation("Completed {Count} access mode transitions", transitions.Count);
    }

    private SubscriptionAccessMode CalculateAccessMode(Subscription subscription, DateTime now)
    {
        // Already blocked = stay blocked
        if (subscription.AccessMode == SubscriptionAccessMode.Blocked)
            return SubscriptionAccessMode.Blocked;

        // Not active = blocked
        if (!subscription.IsActive)
            return SubscriptionAccessMode.Blocked;

        // Lifetime subscriptions never expire
        if (subscription.IsLifetime)
            return SubscriptionAccessMode.Full;

        // Not expired = Full access
        if (subscription.ExpiryDateUtc > now)
            return SubscriptionAccessMode.Full;

        // === EXPIRED - Check grace period ===
        var graceEndDate = subscription.Plan != null && subscription.Plan.GracePeriodDays > 0
            ? subscription.ExpiryDateUtc.AddDays(subscription.Plan.GracePeriodDays)
            : subscription.ExpiryDateUtc;

        if (now <= graceEndDate)
            return SubscriptionAccessMode.GracePeriod;

        // === GRACE PERIOD ENDED ===

        // Check export deadline
        if (subscription.ExportDeadlineUtc.HasValue && now <= subscription.ExportDeadlineUtc.Value)
            return SubscriptionAccessMode.ExportOnly;

        // Check fallback plan (ReadOnly)
        if (subscription.FallbackPlanId.HasValue)
            return SubscriptionAccessMode.ReadOnly;

        // No fallback, no export deadline = Blocked
        return SubscriptionAccessMode.Blocked;
    }

    private async Task ProcessTransitionAsync(
        AccessModeTransition transition,
        ISubscriptionRepository subscriptionRepo,
        IEntitlementService entitlementService,
        IOutboxEventRepository outboxRepo,
        IUnitOfWork unitOfWork,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var subscription = transition.Subscription;

        // Update subscription access mode
        subscription.AccessMode = transition.NewMode;
        subscription.AccessRestrictionMessage = GetRestrictionMessage(transition.NewMode);
        subscription.UpdatedTimestamp = now;

        // Set export deadline if transitioning to ExportOnly and not already set
        if (transition.NewMode == SubscriptionAccessMode.ExportOnly && !subscription.ExportDeadlineUtc.HasValue)
        {
            var exportGraceDays = subscription.Plan?.ExportGraceDays ?? 30;
            subscription.ExportDeadlineUtc = now.AddDays(exportGraceDays);
        }

        await subscriptionRepo.UpdateAsync(subscription);

        // Increment entitlements version to invalidate client caches
        await entitlementService.IncrementVersionAsync(subscription.Id, cancellationToken);

        // Create outbox event for notifications
        await outboxRepo.AddAsync(new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = GetEventType(transition.NewMode),
            CompanyId = subscription.CompanyId,
            SubscriptionId = subscription.Id,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                CompanyName = subscription.Company?.Name ?? "Unknown",
                CompanyEmail = subscription.Company?.ContactEmail,
                PlanName = subscription.Plan?.Name ?? "Unknown",
                OldAccessMode = transition.OldMode.ToString(),
                NewAccessMode = transition.NewMode.ToString(),
                ExportDeadline = subscription.ExportDeadlineUtc,
                TransitionTime = now
            }),
            CreatedAtUtc = now,
            IsProcessed = false,
            AttemptCount = 0
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription {SubscriptionId} (Company: {CompanyId}) transitioned from {OldMode} to {NewMode}",
            subscription.Id, subscription.CompanyId, transition.OldMode, transition.NewMode);
    }

    private static string? GetRestrictionMessage(SubscriptionAccessMode mode)
    {
        return mode switch
        {
            SubscriptionAccessMode.GracePeriod => "Your subscription has expired. You are in a grace period with limited access.",
            SubscriptionAccessMode.ExportOnly => "Your subscription has ended. You can only export your data during this period.",
            SubscriptionAccessMode.ReadOnly => "Your subscription has ended. You have read-only access to your data.",
            SubscriptionAccessMode.Blocked => "Your subscription has ended. Please renew to regain access.",
            _ => null
        };
    }

    private static SubscriptionEventType GetEventType(SubscriptionAccessMode newMode)
    {
        return newMode switch
        {
            SubscriptionAccessMode.GracePeriod => SubscriptionEventType.GracePeriodStarted,
            SubscriptionAccessMode.ExportOnly => SubscriptionEventType.ExportOnlyStarted,
            SubscriptionAccessMode.ReadOnly => SubscriptionEventType.ReadOnlyStarted,
            SubscriptionAccessMode.Blocked => SubscriptionEventType.Blocked,
            _ => SubscriptionEventType.StatusChanged
        };
    }

    /// <summary>
    /// Internal class to track pending transitions
    /// </summary>
    private class AccessModeTransition
    {
        public required Subscription Subscription { get; init; }
        public SubscriptionAccessMode OldMode { get; init; }
        public SubscriptionAccessMode NewMode { get; init; }
    }
}

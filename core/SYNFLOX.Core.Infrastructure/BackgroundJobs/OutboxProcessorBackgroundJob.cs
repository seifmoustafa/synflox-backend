using System;
using System.Text.Json;
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
/// Background job that processes outbox events and sends emails
/// Runs every 5 minutes
/// Implements transactional outbox pattern for reliable event processing
/// </summary>
public class OutboxProcessorBackgroundJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private const int MaxAttempts = 3;

    public OutboxProcessorBackgroundJob(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Background Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events in background job");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor Background Job stopped");
    }

    private async Task ProcessOutboxEventsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var events = await outboxRepo.GetUnprocessedEventsAsync(MaxAttempts);
        var processedCount = 0;
        var failedCount = 0;

        foreach (var outboxEvent in events)
        {
            try
            {
                await ProcessEventAsync(outboxEvent, emailService);
                await outboxRepo.MarkAsProcessedAsync(outboxEvent.Id);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process outbox event {outboxEvent.Id}");
                await outboxRepo.MarkAsFailedAsync(outboxEvent.Id, ex.Message);
                failedCount++;
            }
        }

        if (processedCount > 0 || failedCount > 0)
        {
            _logger.LogInformation($"Processed {processedCount} outbox events, {failedCount} failed");
        }
    }

    private async Task ProcessEventAsync(OutboxEvent outboxEvent, IEmailService emailService)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(outboxEvent.Payload);

        var companyName = payload.GetProperty("CompanyName").GetString() ?? "Unknown";
        var companyEmail = payload.GetProperty("CompanyEmail").GetString();

        if (string.IsNullOrEmpty(companyEmail))
        {
            _logger.LogWarning($"Outbox event {outboxEvent.Id} has no email address");
            return;
        }

        switch (outboxEvent.EventType)
        {
            case SubscriptionEventType.Created:
                var createdPlanName = payload.GetProperty("PlanName").GetString() ?? "";
                var createdExpiryDate = payload.GetProperty("ExpiryDate").GetDateTime();
                var isTrial = payload.GetProperty("IsTrial").GetBoolean();
                await emailService.SendSubscriptionCreatedEmailAsync(
                    companyEmail, companyName, createdPlanName, createdExpiryDate);
                break;

            case SubscriptionEventType.Activated:
                // Use reactivated email for activated subscriptions
                var activatedPlanName = payload.GetProperty("PlanName").GetString() ?? "";
                await emailService.SendSubscriptionReactivatedEmailAsync(
                    companyEmail, companyName, activatedPlanName, "Subscription activated");
                break;

            case SubscriptionEventType.Expired:
                // Use cancelled email for expired subscriptions
                var expiredPlanName = payload.GetProperty("PlanName").GetString() ?? "";
                await emailService.SendSubscriptionCancelledEmailAsync(
                    companyEmail, companyName, expiredPlanName, "Subscription expired");
                break;

            case SubscriptionEventType.Suspended:
                var reason = payload.TryGetProperty("Reason", out var reasonElement) 
                    ? reasonElement.GetString() ?? "Administrative action"
                    : "Administrative action";
                var suspendedPlanName = payload.GetProperty("PlanName").GetString() ?? "";
                await emailService.SendSubscriptionSuspendedEmailAsync(
                    companyEmail, companyName, suspendedPlanName, reason);
                break;

            case SubscriptionEventType.Renewed:
                var renewedPlanName = payload.GetProperty("PlanName").GetString() ?? "";
                var renewedExpiryDate = payload.GetProperty("NewExpiryDate").GetDateTime();
                await emailService.SendSubscriptionRenewedEmailAsync(
                    companyEmail, companyName, renewedPlanName, renewedExpiryDate);
                break;

            case SubscriptionEventType.Upgraded:
                var oldPlanName = payload.GetProperty("OldPlanName").GetString() ?? "";
                var newPlanName = payload.GetProperty("NewPlanName").GetString() ?? "";
                var upgradedExpiryDate = payload.GetProperty("NewExpiryDate").GetDateTime();
                await emailService.SendSubscriptionUpgradedEmailAsync(
                    companyEmail, companyName, oldPlanName, newPlanName, upgradedExpiryDate);
                break;

            case SubscriptionEventType.Canceled:
                var canceledPlanName = payload.GetProperty("PlanName").GetString() ?? "";
                await emailService.SendSubscriptionCancelledEmailAsync(
                    companyEmail, companyName, canceledPlanName, "Subscription canceled");
                break;

            case SubscriptionEventType.TrialStarted:
                // Use created email for trial started
                var trialPlanName = payload.GetProperty("PlanName").GetString() ?? "";
                var trialExpiryDate = payload.GetProperty("ExpiryDate").GetDateTime();
                await emailService.SendSubscriptionCreatedEmailAsync(
                    companyEmail, companyName, trialPlanName, trialExpiryDate);
                break;

            case SubscriptionEventType.AutoRenewed:
                // Use renewed email for auto-renewed subscriptions
                var autoRenewedPlanName = payload.GetProperty("PlanName").GetString() ?? "";
                var autoRenewedExpiryDate = payload.GetProperty("NewExpiryDate").GetDateTime();
                await emailService.SendSubscriptionRenewedEmailAsync(
                    companyEmail, companyName, autoRenewedPlanName, autoRenewedExpiryDate, "Auto-renewal");
                break;

            case SubscriptionEventType.DeferredActivated:
                // Use reactivated email for deferred activated subscriptions
                var deferredNewPlanName = payload.GetProperty("NewPlanName").GetString() ?? "";
                await emailService.SendSubscriptionReactivatedEmailAsync(
                    companyEmail, companyName, deferredNewPlanName, "Deferred activation");
                break;

            default:
                _logger.LogWarning($"Unknown event type: {outboxEvent.EventType}");
                break;
        }
    }
}

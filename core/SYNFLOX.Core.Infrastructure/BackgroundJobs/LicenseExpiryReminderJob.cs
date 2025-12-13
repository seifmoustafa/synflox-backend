using System;
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
/// Background job that sends license expiry reminder emails.
/// Runs daily and checks for:
/// - Licenses expiring in 30 days (first reminder)
/// - Licenses expiring in 7 days (urgent reminder)
/// - Licenses expiring in 1 day (final warning)
/// 
/// Even offline customers receive these emails since they registered
/// with an email address. This ensures they can renew before losing access.
/// </summary>
public class LicenseExpiryReminderJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LicenseExpiryReminderJob> _logger;
    
    // Run once per day at 9:00 AM UTC
    private readonly TimeSpan _runTime = new(9, 0, 0);
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public LicenseExpiryReminderJob(
        IServiceProvider serviceProvider,
        ILogger<LicenseExpiryReminderJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("License Expiry Reminder Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Only run near the scheduled time
                if (Math.Abs((now.TimeOfDay - _runTime).TotalMinutes) < 30)
                {
                    await ProcessExpiryRemindersAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in License Expiry Reminder Job");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("License Expiry Reminder Job stopped");
    }

    private async Task ProcessExpiryRemindersAsync()
    {
        _logger.LogInformation("Starting license expiry reminder processing");

        using var scope = _serviceProvider.CreateScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var today = now.Date;

        // Process 30-day reminders
        await ProcessRemindersForDays(subscriptionRepo, outboxRepo, unitOfWork, 30, "30DayReminder", now);

        // Process 7-day reminders
        await ProcessRemindersForDays(subscriptionRepo, outboxRepo, unitOfWork, 7, "7DayReminder", now);

        // Process 1-day reminders (final warning)
        await ProcessRemindersForDays(subscriptionRepo, outboxRepo, unitOfWork, 1, "1DayReminder", now);

        _logger.LogInformation("Completed license expiry reminder processing");
    }

    private async Task ProcessRemindersForDays(
        ISubscriptionRepository subscriptionRepo,
        IOutboxEventRepository outboxRepo,
        IUnitOfWork unitOfWork,
        int daysUntilExpiry,
        string reminderType,
        DateTime now)
    {
        var targetDate = now.Date.AddDays(daysUntilExpiry);
        var nextDay = targetDate.AddDays(1);

        // Find subscriptions with licenses expiring on the target date
        var subscriptions = await subscriptionRepo.FindAsync(s =>
            !s.IsDeleted &&
            s.IsActive &&
            !s.IsExpired &&
            !string.IsNullOrEmpty(s.OfflineLicenseKey) &&
            s.ExpiryDateUtc >= targetDate &&
            s.ExpiryDateUtc < nextDay);

        var count = 0;
        foreach (var subscription in subscriptions)
        {
            // Skip lifetime subscriptions
            if (subscription.IsLifetime)
                continue;

            // Skip if company has no email
            if (string.IsNullOrEmpty(subscription.Company?.ContactEmail))
                continue;

            // Check if we already sent this reminder today (idempotency)
            // Using GetBySubscriptionIdAsync and filtering since FindAsync is not available
            var existingEvents = await outboxRepo.GetBySubscriptionIdAsync(subscription.Id);
            var alreadySentToday = existingEvents.Any(e =>
                e.EventType == GetEventType(reminderType) &&
                e.CreatedAtUtc >= now.Date &&
                e.CreatedAtUtc < now.Date.AddDays(1));

            if (alreadySentToday)
                continue;

            // Create outbox event for email
            await outboxRepo.AddAsync(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = GetEventType(reminderType),
                CompanyId = subscription.CompanyId,
                SubscriptionId = subscription.Id,
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    CompanyName = subscription.Company.Name,
                    CompanyEmail = subscription.Company.ContactEmail,
                    PlanName = subscription.Plan?.Name ?? "Unknown Plan",
                    ExpiryDate = subscription.ExpiryDateUtc,
                    DaysUntilExpiry = daysUntilExpiry,
                    ReminderType = reminderType,
                    HasLicenseKey = !string.IsNullOrEmpty(subscription.OfflineLicenseKey)
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
            _logger.LogInformation("Created {Count} {ReminderType} expiry reminders", count, reminderType);
        }
    }

    private static SubscriptionEventType GetEventType(string reminderType)
    {
        // Use StatusChanged event type with specific payload to identify reminder type
        // In a more complete implementation, you'd add specific event types
        return SubscriptionEventType.StatusChanged;
    }
}

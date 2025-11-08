using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities.Licensing;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Context;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Background;

/// <summary>
/// Background service that periodically checks for expired subscriptions and updates their status.
/// </summary>
public class SubscriptionExpiryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionExpiryWorker> _logger;
    private readonly ExpiryCheckSettings _settings;

    public SubscriptionExpiryWorker(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionExpiryWorker> logger,
        IOptions<ExpiryCheckSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Subscription expiry worker is disabled");
            return;
        }

        _logger.LogInformation(
            "Subscription expiry worker started. Check interval: {Interval} hours, Check time: {Time}",
            _settings.CheckIntervalHours, _settings.CheckTime);

        // Calculate initial delay to run at the specified time
        var initialDelay = CalculateInitialDelay();
        if (initialDelay > TimeSpan.Zero)
        {
            _logger.LogInformation("Waiting {Delay} before first expiry check", initialDelay);
            await Task.Delay(initialDelay, stoppingToken);
        }

        var interval = TimeSpan.FromHours(_settings.CheckIntervalHours);
        var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAndUpdateExpiredCompaniesAsync(stoppingToken);
        }
    }

    private TimeSpan CalculateInitialDelay()
    {
        if (!TimeSpan.TryParse(_settings.CheckTime, out var targetTime))
        {
            _logger.LogWarning("Invalid CheckTime format: {Time}. Using default: 00:00", _settings.CheckTime);
            targetTime = TimeSpan.Zero;
        }

        var now = DateTime.UtcNow;
        var todayTarget = now.Date.Add(targetTime);
        var tomorrowTarget = todayTarget.AddDays(1);

        var target = now < todayTarget ? todayTarget : tomorrowTarget;
        return target - now;
    }

    private async Task CheckAndUpdateExpiredCompaniesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var now = DateTime.UtcNow;

            // Find companies that have expired but IsExpired flag is not set
            // This includes both regular subscriptions and trial subscriptions
            var expiredCompanies = await dbContext.Companies
                .Where(c => !c.IsDeleted &&
                           !c.IsExpired &&
                           c.IsActive &&
                           ((c.ExpiryDate.HasValue && c.ExpiryDate.Value < now) ||
                            (c.IsTrial && c.TrialEndDate.HasValue && c.TrialEndDate.Value < now)))
                .ToListAsync(cancellationToken);

            if (expiredCompanies.Count == 0)
            {
                _logger.LogDebug("No expired companies found");
                return;
            }

            _logger.LogInformation("Found {Count} expired companies to update", expiredCompanies.Count);

            var historyService = scope.ServiceProvider.GetRequiredService<ISubscriptionHistoryService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
            var localizer = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
            var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
            var idEncryption = scope.ServiceProvider.GetRequiredService<IIdEncryptionService>();

            foreach (var company in expiredCompanies)
            {
                try
                {
                            var oldIsExpired = company.IsExpired;
                            var oldIsActive = company.IsActive;
                            var wasTrial = company.IsTrial;

                            company.IsExpired = true;
                            company.IsActive = false;
                            
                            // If it was a trial, mark it as no longer a trial
                            if (company.IsTrial && company.TrialEndDate.HasValue && company.TrialEndDate.Value < now)
                            {
                                company.IsTrial = false;
                            }
                            
                            company.UpdatedTimestamp = DateTime.UtcNow;

                    // Log to history
                    await historyService.LogSubscriptionEventAsync(
                        company.Id,
                        SubscriptionHistoryActionType.Expired,
                        new { IsExpired = oldIsExpired, IsActive = oldIsActive, ExpiryDate = company.ExpiryDate },
                        new { IsExpired = company.IsExpired, IsActive = company.IsActive, ExpiryDate = company.ExpiryDate },
                        null, // System action, no user
                        $"Subscription expired. ExpiryDate: {company.ExpiryDate:yyyy-MM-dd}");

                    // Create notification
                    try
                    {
                        var title = localizer["Notification.ExpiredTitle"];
                        var message = string.Format(localizer["Notification.ExpiredMessage"], company.Name, company.ExpiryDate.Value.ToString("yyyy-MM-dd"));
                        await notificationService.CreateNotificationAsync(
                            company.Id,
                            Domain.Enums.NotificationType.Expired,
                            title,
                            message);

                        // Send email if enabled and email exists
                        if (!string.IsNullOrWhiteSpace(company.ContactEmail))
                        {
                            var emailSubject = string.Format(localizer["Notification.ExpiredEmailSubject"], company.Name);
                            var emailBody = string.Format(localizer["Notification.ExpiredEmailBody"], company.Name, company.ExpiryDate.Value.ToString("yyyy-MM-dd"));
                            await emailQueue.EnqueueAsync(company.ContactEmail, emailSubject, emailBody);
                        }

                        // Trigger webhook
                        try
                        {
                            var webhookPayload = new { CompanyId = idEncryption.Encrypt(company.Id), CompanyName = company.Name, ExpiryDate = company.ExpiryDate.Value.ToString("yyyy-MM-dd"), EventType = "CompanyExpired" };
                            await webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanyExpired, webhookPayload);
                        }
                        catch (Exception webhookEx)
                        {
                            _logger.LogWarning(webhookEx, "Failed to trigger webhook for expired company {CompanyId}", company.Id);
                        }
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogWarning(notifEx, "Failed to create notification for expired company {CompanyId}", company.Id);
                    }

                    _logger.LogInformation(
                        "Marked company {CompanyId} ({Name}) as expired. ExpiryDate: {ExpiryDate}",
                        company.Id, company.Name, company.ExpiryDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update expired status for company {CompanyId}", company.Id);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully updated {Count} expired companies", expiredCompanies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during subscription expiry check");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription expiry worker stopping");
        await base.StopAsync(cancellationToken);
    }
}


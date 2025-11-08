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
/// Background service that sends expiry notifications (email and in-system) for companies.
/// </summary>
public class ExpiryNotificationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiryNotificationWorker> _logger;
    private readonly NotificationSettings _settings;

    public ExpiryNotificationWorker(
        IServiceProvider serviceProvider,
        ILogger<ExpiryNotificationWorker> logger,
        IOptions<NotificationSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Expiry notification worker is disabled");
            return;
        }

        _logger.LogInformation(
            "Expiry notification worker started. Check time: {Time}, Warning days: {Days}",
            _settings.CheckTime, string.Join(", ", _settings.ExpiryWarningDays));

        // Calculate initial delay to run at the specified time
        var initialDelay = CalculateInitialDelay();
        if (initialDelay > TimeSpan.Zero)
        {
            _logger.LogInformation("Waiting {Delay} before first notification check", initialDelay);
            await Task.Delay(initialDelay, stoppingToken);
        }

        // Run daily
        var interval = TimeSpan.FromDays(1);
        var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAndSendNotificationsAsync(stoppingToken);
        }
    }

    private TimeSpan CalculateInitialDelay()
    {
        if (!TimeSpan.TryParse(_settings.CheckTime, out var targetTime))
        {
            _logger.LogWarning("Invalid CheckTime format: {Time}. Using default: 09:00", _settings.CheckTime);
            targetTime = new TimeSpan(9, 0, 0);
        }

        var now = DateTime.UtcNow;
        var todayTarget = now.Date.Add(targetTime);
        var tomorrowTarget = todayTarget.AddDays(1);

        var target = now < todayTarget ? todayTarget : tomorrowTarget;
        return target - now;
    }

    private async Task CheckAndSendNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
            var localizer = scope.ServiceProvider.GetRequiredService<ILocalizationService>();

            var now = DateTime.UtcNow;
            var companies = await dbContext.Companies
                .Where(c => !c.IsDeleted && c.ExpiryDate.HasValue)
                .ToListAsync(cancellationToken);

            if (companies.Count == 0)
            {
                _logger.LogDebug("No companies with expiry dates found");
                return;
            }

            _logger.LogInformation("Checking {Count} companies for expiry notifications", companies.Count);

            int emailCount = 0;
            int notificationCount = 0;

            foreach (var company in companies)
            {
                if (!company.ExpiryDate.HasValue) continue;

                var daysUntilExpiry = (company.ExpiryDate.Value.Date - now.Date).Days;
                var isExpired = company.ExpiryDate.Value < now;

                // Check if we should send warning notifications
                if (!isExpired && _settings.ExpiryWarningDays.Contains(daysUntilExpiry))
                {
                    await SendExpiryWarningAsync(company, daysUntilExpiry, notificationService, emailQueue, localizer);
                    emailCount++;
                    notificationCount++;
                }
                // Send expired notification if expired today
                else if (isExpired && company.ExpiryDate.Value.Date == now.Date)
                {
                    await SendExpiredNotificationAsync(company, notificationService, emailQueue, localizer);
                    emailCount++;
                    notificationCount++;
                }
            }

            _logger.LogInformation(
                "Notification check completed: {EmailCount} emails sent, {NotificationCount} in-system notifications created",
                emailCount, notificationCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during expiry notification check");
        }
    }

    private async Task SendExpiryWarningAsync(
        Domain.Entities.Licensing.Company company,
        int daysUntilExpiry,
        INotificationService notificationService,
        IEmailQueue emailQueue,
        ILocalizationService localizer)
    {
        try
        {
            var title = string.Format(localizer["Notification.ExpiryWarningTitle"], daysUntilExpiry);
            var message = string.Format(localizer["Notification.ExpiryWarningMessage"], company.Name, daysUntilExpiry, company.ExpiryDate.Value.ToString("yyyy-MM-dd"));

            // Create in-system notification
            if (_settings.InSystemEnabled)
            {
                await notificationService.CreateNotificationAsync(
                    company.Id,
                    NotificationType.ExpiryWarning,
                    title,
                    message);
            }

            // Send email notification
            if (_settings.EmailEnabled && !string.IsNullOrWhiteSpace(company.ContactEmail))
            {
                var emailSubject = string.Format(localizer["Notification.ExpiryWarningEmailSubject"], company.Name, daysUntilExpiry);
                var emailBody = string.Format(localizer["Notification.ExpiryWarningEmailBody"], company.Name, daysUntilExpiry, company.ExpiryDate.Value.ToString("yyyy-MM-dd"));

                await emailQueue.EnqueueAsync(company.ContactEmail, emailSubject, emailBody);
            }

            _logger.LogInformation(
                "Sent expiry warning notification for company {CompanyId} ({Name}) - {Days} days until expiry",
                company.Id, company.Name, daysUntilExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expiry warning for company {CompanyId}", company.Id);
        }
    }

    private async Task SendExpiredNotificationAsync(
        Domain.Entities.Licensing.Company company,
        INotificationService notificationService,
        IEmailQueue emailQueue,
        ILocalizationService localizer)
    {
        try
        {
            var title = localizer["Notification.ExpiredTitle"];
            var message = string.Format(localizer["Notification.ExpiredMessage"], company.Name, company.ExpiryDate.Value.ToString("yyyy-MM-dd"));

            // Create in-system notification
            if (_settings.InSystemEnabled)
            {
                await notificationService.CreateNotificationAsync(
                    company.Id,
                    NotificationType.Expired,
                    title,
                    message);
            }

            // Send email notification
            if (_settings.EmailEnabled && !string.IsNullOrWhiteSpace(company.ContactEmail))
            {
                var emailSubject = string.Format(localizer["Notification.ExpiredEmailSubject"], company.Name);
                var emailBody = string.Format(localizer["Notification.ExpiredEmailBody"], company.Name, company.ExpiryDate.Value.ToString("yyyy-MM-dd"));

                await emailQueue.EnqueueAsync(company.ContactEmail, emailSubject, emailBody);
            }

            _logger.LogInformation(
                "Sent expired notification for company {CompanyId} ({Name})",
                company.Id, company.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expired notification for company {CompanyId}", company.Id);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Expiry notification worker stopping");
        await base.StopAsync(cancellationToken);
    }
}


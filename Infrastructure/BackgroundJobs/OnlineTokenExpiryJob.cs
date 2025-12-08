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
/// Background job that monitors online client token expiry.
/// 
/// Responsibilities:
/// - Notify admins about tokens expiring soon (7 days, 1 day)
/// - Auto-refresh tokens if enabled (before expiry)
/// - Clean up expired tokens (soft delete)
/// 
/// Runs every 6 hours.
/// </summary>
public class OnlineTokenExpiryJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OnlineTokenExpiryJob> _logger;
    
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public OnlineTokenExpiryJob(
        IServiceProvider serviceProvider,
        ILogger<OnlineTokenExpiryJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Online Token Expiry Job started");

        // Initial delay to allow system to fully start
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTokenExpiryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Online Token Expiry Job");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Online Token Expiry Job stopped");
    }

    private async Task ProcessTokenExpiryAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting online token expiry processing");

        using var scope = _serviceProvider.CreateScope();
        var tokenRepo = scope.ServiceProvider.GetRequiredService<IOnlineClientTokenRepository>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var processedCount = 0;

        // Process tokens expiring in 7 days (first warning)
        var expiringIn7Days = await tokenRepo.GetExpiringTokensAsync(7, cancellationToken);
        foreach (var token in expiringIn7Days.Where(t => t.DaysUntilExpiry >= 6))
        {
            await CreateExpiryNotificationAsync(outboxRepo, token, 7, now);
            processedCount++;
        }

        // Process tokens expiring in 1 day (final warning)
        var expiringIn1Day = await tokenRepo.GetExpiringTokensAsync(1, cancellationToken);
        foreach (var token in expiringIn1Day.Where(t => t.DaysUntilExpiry == 0 || t.DaysUntilExpiry == 1))
        {
            await CreateExpiryNotificationAsync(outboxRepo, token, 1, now);
            processedCount++;
        }

        // Auto-refresh tokens if enabled (expiring within 3 days)
        var expiringIn3Days = await tokenRepo.GetExpiringTokensAsync(3, cancellationToken);
        foreach (var token in expiringIn3Days.Where(t => t.AutoRefreshEnabled && t.DaysUntilExpiry <= 3))
        {
            try
            {
                await AutoRefreshTokenAsync(token, tokenRepo, unitOfWork, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-refresh token {TokenId}", token.Id);
            }
        }

        if (processedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Processed {Count} token expiry actions", processedCount);
        }

        _logger.LogInformation("Completed online token expiry processing");
    }

    private async Task CreateExpiryNotificationAsync(
        IOutboxEventRepository outboxRepo,
        Domain.Entities.OnlineAccess.OnlineClientToken token,
        int daysUntilExpiry,
        DateTime now)
    {
        // Create outbox event for notification
        await outboxRepo.AddAsync(new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = SubscriptionEventType.StatusChanged, // Could add TokenExpiringSoon event type
            CompanyId = token.CompanyId,
            SubscriptionId = token.SubscriptionId,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                NotificationType = "OnlineTokenExpiring",
                TokenId = token.Id,
                TokenName = token.Name,
                ExpiryDate = token.ExpiresAtUtc,
                DaysUntilExpiry = daysUntilExpiry,
                AutoRefreshEnabled = token.AutoRefreshEnabled,
                CompanyName = token.Company?.Name ?? "Unknown"
            }),
            CreatedAtUtc = now,
            IsProcessed = false,
            AttemptCount = 0
        });

        _logger.LogDebug("Created expiry notification for token {TokenId}, expires in {Days} days", 
            token.Id, daysUntilExpiry);
    }

    private async Task AutoRefreshTokenAsync(
        Domain.Entities.OnlineAccess.OnlineClientToken token,
        IOnlineClientTokenRepository tokenRepo,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        // Extend token expiry by the original duration (calculated from IssuedAt to original ExpiresAt)
        var originalDuration = token.ExpiresAtUtc - token.IssuedAtUtc;
        var newExpiry = DateTime.UtcNow.Add(originalDuration);

        // Don't extend beyond subscription expiry
        if (token.Subscription != null && newExpiry > token.Subscription.ExpiryDateUtc)
        {
            newExpiry = token.Subscription.ExpiryDateUtc;
        }

        token.ExpiresAtUtc = newExpiry;
        token.UpdatedTimestamp = DateTime.UtcNow;

        await tokenRepo.UpdateAsync(token, cancellationToken);

        _logger.LogInformation("Auto-refreshed token {TokenId}, new expiry: {NewExpiry}", 
            token.Id, newExpiry);
    }
}

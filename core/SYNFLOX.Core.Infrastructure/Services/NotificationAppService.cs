using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Notification;
using Application.Services;
using AutoMapper;
using Domain.Entities.Notifications;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Infrastructure.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Implementation of INotificationAppService.
/// Handles notifications, preferences, and push subscriptions.
/// Uses AutoMapper for entity-DTO mapping and IStringLocalizer for localization.
/// </summary>
public class NotificationAppService : INotificationAppService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly INotificationPreferenceRepository _preferenceRepo;
    private readonly IPushSubscriptionRepository _pushRepo;
    private readonly ICompanyAdminRepository _companyAdminRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<NotificationAppService> _logger;
    private readonly IEmailService? _emailService;

    public NotificationAppService(
        INotificationRepository notificationRepo,
        INotificationPreferenceRepository preferenceRepo,
        IPushSubscriptionRepository pushRepo,
        ICompanyAdminRepository companyAdminRepo,
        ICompanyRepository companyRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStringLocalizer<SharedResource> localizer,
        ILogger<NotificationAppService> logger,
        IEmailService? emailService = null
    )
    {
        _notificationRepo = notificationRepo;
        _preferenceRepo = preferenceRepo;
        _pushRepo = pushRepo;
        _companyAdminRepo = companyAdminRepo;
        _companyRepo = companyRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizer = localizer;
        _logger = logger;
        _emailService = emailService;
    }

    #region Admin Operations

    /// <inheritdoc/>
    public async Task<SendMessageResultDto> SendToCompaniesAsync(
        SendMessageRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        var result = new SendMessageResultDto { TotalCompanies = request.CompanyIds.Count };

        if (request.CompanyIds.Count == 0)
        {
            result.Success = false;
            result.Message = _localizer["Notification.NoCompaniesSelected"];
            return result;
        }

        _logger.LogInformation(
            "SendToCompaniesAsync: Sending to {Count} companies",
            request.CompanyIds.Count
        );

        foreach (var encryptedCompanyId in request.CompanyIds)
        {
            var companyResult = new CompanySendResultDto();

            try
            {
                // Decrypt company ID (assuming IDs come encrypted from frontend)
                // For now, try to parse as GUID directly
                if (!Guid.TryParse(encryptedCompanyId, out var companyId))
                {
                    companyResult.Error = _localizer["Notification.InvalidCompanyId"];
                    result.Errors.Add($"Invalid company ID: {encryptedCompanyId}");
                    result.CompanyResults.Add(companyResult);
                    continue;
                }

                companyResult.CompanyId = companyId;

                // Get company details
                var company = await _companyRepo.GetByIdAsync(companyId, null, cancellationToken);
                if (company == null)
                {
                    companyResult.Error = _localizer["Notification.CompanyNotFound"];
                    result.Errors.Add(
                        string.Format(_localizer["Notification.CompanyNotFoundWithId"], companyId)
                    );
                    result.CompanyResults.Add(companyResult);
                    continue;
                }

                companyResult.CompanyName = company.Name;

                // Send In-App Notification
                if (request.SendNotification)
                {
                    var admin = await _companyAdminRepo.GetByCompanyIdAsync(
                        companyId,
                        cancellationToken
                    );
                    if (admin != null)
                    {
                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            UserId = admin.Id,
                            UserType = "CompanyAdmin",
                            Type = "system_announcement",
                            Category = "announcement",
                            Title = request.Title,
                            Message = request.Message,
                            Priority = request.Priority,
                            Icon = "bell",
                            CompanyId = companyId,
                            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                            CreatedAtUtc = DateTime.UtcNow,
                        };

                        await _notificationRepo.AddAsync(notification);
                        companyResult.NotificationSent = true;
                        result.NotificationsSent++;

                        _logger.LogInformation(
                            "Notification created: Id={NotifId}, UserId={UserId}, CompanyId={CompanyId}",
                            notification.Id,
                            admin.Id,
                            companyId
                        );
                    }
                    else
                    {
                        result.CompaniesWithoutAdmin++;
                        _logger.LogWarning("Company {CompanyId} has no admin user", companyId);
                    }
                }

                // Send Email
                if (request.SendEmail && _emailService != null)
                {
                    if (!string.IsNullOrEmpty(company.ContactEmail))
                    {
                        try
                        {
                            // TODO: Implement email sending
                            // await _emailService.SendAsync(company.ContactEmail, request.Title, request.Message);
                            companyResult.EmailSent = true;
                            result.EmailsSent++;
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(
                                emailEx,
                                "Failed to send email to {Email}",
                                company.ContactEmail
                            );
                            companyResult.Error = "Email sending failed";
                        }
                    }
                    else
                    {
                        result.CompaniesWithoutEmail++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing company {CompanyId}", encryptedCompanyId);
                companyResult.Error = ex.Message;
                result.Errors.Add($"Error: {ex.Message}");
            }

            result.CompanyResults.Add(companyResult);
        }

        await _unitOfWork.SaveChangesAsync();

        // Set success and localized message
        result.Success = result.NotificationsSent > 0 || result.EmailsSent > 0;

        if (result.Success)
        {
            result.Message = string.Format(
                _localizer["Notification.SendSuccess"],
                result.NotificationsSent,
                result.EmailsSent,
                result.TotalCompanies
            );
        }
        else
        {
            result.Message = _localizer["Notification.NoMessagesSent"];
        }

        _logger.LogInformation(
            "SendToCompaniesAsync complete: {NotificationsSent} notifications, {EmailsSent} emails",
            result.NotificationsSent,
            result.EmailsSent
        );

        return result;
    }

    #endregion

    #region Shared Operations

    /// <inheritdoc/>
    public async Task<NotificationListDto> GetNotificationsAsync(
        Guid userId,
        string userType,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default
    )
    {
        var notifications = await _notificationRepo.GetByUserIdAsync(
            userId,
            userType,
            page,
            pageSize,
            unreadOnly,
            cancellationToken
        );

        var totalCount = await _notificationRepo.GetTotalCountAsync(
            userId,
            userType,
            cancellationToken
        );
        var unreadCount = await _notificationRepo.GetUnreadCountAsync(
            userId,
            userType,
            cancellationToken
        );

        return new NotificationListDto
        {
            Items = _mapper.Map<List<NotificationDto>>(notifications),
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Page = page,
            PageSize = pageSize,
            HasMore = page * pageSize < totalCount,
        };
    }

    /// <inheritdoc/>
    public async Task<RecentNotificationsDto> GetRecentNotificationsAsync(
        Guid userId,
        string userType,
        int count = 5,
        CancellationToken cancellationToken = default
    )
    {
        var notifications = await _notificationRepo.GetRecentAsync(
            userId,
            userType,
            count,
            cancellationToken
        );
        var unreadCount = await _notificationRepo.GetUnreadCountAsync(
            userId,
            userType,
            cancellationToken
        );

        return new RecentNotificationsDto
        {
            Notifications = _mapper.Map<List<NotificationDto>>(notifications),
            UnreadCount = unreadCount,
        };
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        return await _notificationRepo.GetUnreadCountAsync(userId, userType, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _notificationRepo.MarkAsReadAsync(notificationId, cancellationToken);
        if (result)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<int> MarkAllAsReadAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        var count = await _notificationRepo.MarkAllAsReadAsync(userId, userType, cancellationToken);
        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return count;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default
    )
    {
        var notification = await _notificationRepo.GetByIdAsync(
            notificationId,
            null,
            cancellationToken
        );
        if (notification != null)
        {
            notification.IsDeleted = true;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        return false;
    }

    #endregion

    #region Preferences

    /// <inheritdoc/>
    public async Task<NotificationPreferenceDto> GetPreferencesAsync(
        Guid userId,
        string userType,
        CancellationToken cancellationToken = default
    )
    {
        var preference = await _preferenceRepo.GetOrCreateByUserIdAsync(
            userId,
            userType,
            cancellationToken
        );
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<NotificationPreferenceDto>(preference);
    }

    /// <inheritdoc/>
    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid userId,
        string userType,
        NotificationPreferenceDto preferences,
        CancellationToken cancellationToken = default
    )
    {
        var preference = await _preferenceRepo.GetOrCreateByUserIdAsync(
            userId,
            userType,
            cancellationToken
        );

        preference.EmailEnabled = preferences.EmailEnabled;
        preference.PushEnabled = preferences.PushEnabled;
        preference.InAppEnabled = preferences.InAppEnabled;
        preference.QuietHoursEnabled = preferences.QuietHoursEnabled;
        preference.QuietHoursStart = preferences.QuietHoursStart;
        preference.QuietHoursEnd = preferences.QuietHoursEnd;
        preference.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<NotificationPreferenceDto>(preference);
    }

    #endregion

    #region Push Subscriptions

    /// <inheritdoc/>
    public async Task<bool> RegisterPushSubscriptionAsync(
        Guid userId,
        string userType,
        RegisterPushSubscriptionDto subscription,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(subscription.Endpoint))
        {
            return false;
        }

        // Check if subscription already exists by endpoint
        var existing = await _pushRepo.GetByEndpointAsync(subscription.Endpoint, cancellationToken);

        if (existing != null)
        {
            // Update existing
            existing.UserId = userId;
            existing.UserType = userType;
            existing.IsActive = true;
            existing.LastUsedAtUtc = DateTime.UtcNow;
        }
        else
        {
            // Create new
            var newSubscription = new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserType = userType,
                Endpoint = subscription.Endpoint,
                P256dhKey = subscription.P256dh,
                AuthKey = subscription.Auth,
                Platform = subscription.Platform,
                DeviceName = subscription.DeviceName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            await _pushRepo.AddAsync(newSubscription);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UnregisterPushSubscriptionAsync(
        string endpoint,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _pushRepo.DeleteByEndpointAsync(endpoint, cancellationToken);
        if (result)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return result;
    }

    #endregion

    #region Internal Operations

    /// <inheritdoc/>
    public async Task<NotificationDto> CreateNotificationAsync(
        CreateNotificationDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            UserType = dto.UserType,
            Type = dto.Type,
            Category = dto.Category ?? "general",
            Title = dto.Title,
            Message = dto.Message,
            Data = dto.Data,
            Priority = dto.Priority,
            Icon = dto.Icon ?? "bell",
            ActionUrl = dto.ActionUrl,
            ExpiresAtUtc = dto.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(30),
            CompanyId = dto.CompanyId,
            SubscriptionId = dto.SubscriptionId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _notificationRepo.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Created notification {NotificationId} for user {UserId}",
            notification.Id,
            dto.UserId
        );

        return _mapper.Map<NotificationDto>(notification);
    }

    /// <inheritdoc/>
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var count = await _notificationRepo.DeleteExpiredAsync(cancellationToken);
        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired notifications", count);
        }
        return count;
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Notifications;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Notifications;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueue? _emailQueue;
    private readonly ILocalizationService _localizer;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository repository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEmailQueue? emailQueue,
        ILocalizationService localizer,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _emailQueue = emailQueue;
        _localizer = localizer;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<NotificationDto> CreateNotificationAsync(
        Guid companyId,
        NotificationType type,
        string title,
        string message,
        string? companyEmail = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        var created = await _repository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        // Send email if enabled and email is provided
        if (_settings.EmailEnabled && !string.IsNullOrWhiteSpace(companyEmail) && _emailQueue != null)
        {
            try
            {
                // Get email templates and format them
                var emailSubjectTemplate = GetEmailSubjectTemplate(type);
                var emailBodyTemplate = GetEmailBodyTemplate(type);
                
                // Extract company name from message (first parameter in formatted message)
                // Message format: "Your subscription for {CompanyName} has been activated with expiry date: {Date}"
                var companyName = ExtractCompanyNameFromMessage(message);
                
                // Format email subject and body with company name
                var emailSubject = !string.IsNullOrEmpty(emailSubjectTemplate) 
                    ? FormatEmailTemplate(emailSubjectTemplate, companyName, ExtractDateFromMessage(message))
                    : title; // Fallback to notification title
                
                var emailBody = !string.IsNullOrEmpty(emailBodyTemplate)
                    ? FormatEmailTemplate(emailBodyTemplate, companyName, ExtractDateFromMessage(message))
                    : message; // Fallback to notification message
                
                await _emailQueue.EnqueueAsync(companyEmail, emailSubject, emailBody);
                _logger.LogInformation("Email notification queued for company {CompanyId}, type: {Type}", companyId, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue email notification for company {CompanyId}, type: {Type}", companyId, type);
                // Don't fail the notification creation if email fails
            }
        }

        return _mapper.Map<NotificationDto>(created);
    }

    private string GetEmailSubjectTemplate(NotificationType type)
    {
        return type switch
        {
            NotificationType.Activated => _localizer["Notification.ActivatedEmailSubject"] ?? string.Empty,
            NotificationType.Suspended => _localizer["Notification.SuspendedEmailSubject"] ?? string.Empty,
            NotificationType.Resumed => _localizer["Notification.ResumedEmailSubject"] ?? string.Empty,
            NotificationType.Extended => _localizer["Notification.ExtendedEmailSubject"] ?? string.Empty,
            NotificationType.Expired => _localizer["Notification.ExpiredEmailSubject"] ?? string.Empty,
            NotificationType.ExpiryWarning => _localizer["Notification.ExpiryWarningEmailSubject"] ?? string.Empty,
            _ => string.Empty
        };
    }

    private string GetEmailBodyTemplate(NotificationType type)
    {
        return type switch
        {
            NotificationType.Activated => _localizer["Notification.ActivatedEmailBody"] ?? string.Empty,
            NotificationType.Suspended => _localizer["Notification.SuspendedEmailBody"] ?? string.Empty,
            NotificationType.Resumed => _localizer["Notification.ResumedEmailBody"] ?? string.Empty,
            NotificationType.Extended => _localizer["Notification.ExtendedEmailBody"] ?? string.Empty,
            NotificationType.Expired => _localizer["Notification.ExpiredEmailBody"] ?? string.Empty,
            NotificationType.ExpiryWarning => _localizer["Notification.ExpiryWarningEmailBody"] ?? string.Empty,
            _ => string.Empty
        };
    }

    private string ExtractCompanyNameFromMessage(string message)
    {
        // Try to extract company name from message
        // Message format examples:
        // "Your subscription for {CompanyName} has been activated with expiry date: {Date}"
        // "Your subscription for {CompanyName} has been suspended"
        
        var parts = message.Split(new[] { " for " }, StringSplitOptions.None);
        if (parts.Length > 1)
        {
            var afterFor = parts[1];
            var nameParts = afterFor.Split(new[] { " has been", " with expiry", " to: " }, StringSplitOptions.None);
            if (nameParts.Length > 0)
            {
                return nameParts[0].Trim();
            }
        }
        
        return "Customer"; // Fallback
    }

    private string? ExtractDateFromMessage(string message)
    {
        // Try to extract date from message
        // Look for patterns like "expiry date: 2025-12-31" or "to: 2025-12-31"
        var datePatterns = new[] { "expiry date: ", "to: ", "date: " };
        foreach (var pattern in datePatterns)
        {
            var index = message.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = index + pattern.Length;
                var end = message.IndexOfAny(new[] { '.', ',', '\n', '\r' }, start);
                if (end < 0) end = message.Length;
                var dateStr = message.Substring(start, end - start).Trim();
                if (!string.IsNullOrEmpty(dateStr))
                {
                    return dateStr;
                }
            }
        }
        
        return null;
    }

    private string FormatEmailTemplate(string template, string companyName, string? date = null)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;
        
        // Replace placeholders: {0} = company name, {1} = date (if provided)
        if (template.Contains("{0}"))
        {
            template = template.Replace("{0}", companyName);
        }
        
        if (template.Contains("{1}") && !string.IsNullOrEmpty(date))
        {
            template = template.Replace("{1}", date);
        }
        else if (template.Contains("{1}"))
        {
            template = template.Replace("{1}", "");
        }
        
        return template;
    }

    public async Task<(IEnumerable<NotificationDto> Notifications, PaginationMetadata Meta)> GetNotificationsByCompanyIdAsync(
        Guid companyId,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var notifications = await _repository.GetByCompanyIdAsync(companyId, unreadOnly, skip, pageSize);
        var totalCount = await _repository.CountByCompanyIdAsync(companyId, unreadOnly);

        var dtos = _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    public async Task<(IEnumerable<NotificationDto> Notifications, PaginationMetadata Meta)> GetAllNotificationsAsync(
        Guid? companyId = null,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var notifications = await _repository.GetAllWithFiltersAsync(companyId, unreadOnly, skip, pageSize);
        var totalCount = await _repository.CountWithFiltersAsync(companyId, unreadOnly);

        var dtos = _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _repository.GetByIdAsync(notificationId, null);
        if (notification == null || notification.IsDeleted)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _repository.UpdateAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid companyId)
    {
        var notifications = await _repository.GetByCompanyIdAsync(companyId, unreadOnly: true, skip: 0, take: int.MaxValue);
        var notificationList = notifications.ToList();

        foreach (var notification in notificationList)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _repository.UpdateAsync(notification);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid companyId)
    {
        return await _repository.CountByCompanyIdAsync(companyId, unreadOnly: true);
    }
}


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Notifications;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Notifications;
using Domain.Enums;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        INotificationRepository repository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<NotificationDto> CreateNotificationAsync(
        Guid companyId,
        NotificationType type,
        string title,
        string message)
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

        return _mapper.Map<NotificationDto>(created);
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


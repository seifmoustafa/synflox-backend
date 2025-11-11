using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.SubscriptionHistory;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Enums;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class SubscriptionHistoryService : ISubscriptionHistoryService
{
    private readonly ISubscriptionHistoryRepository _repository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionHistoryService(
        ISubscriptionHistoryRepository repository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<(IEnumerable<SubscriptionHistoryDto> History, PaginationMetadata Meta)> GetHistoryByCompanyIdAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var history = await _repository.GetByCompanyIdAsync(companyId, skip, pageSize);
        var totalCount = await _repository.CountByCompanyIdAsync(companyId);

        var dtos = _mapper.Map<IEnumerable<SubscriptionHistoryDto>>(history);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    public async Task<(IEnumerable<SubscriptionHistoryDto> History, PaginationMetadata Meta)> GetHistoryByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? companyId = null,
        int page = 1,
        int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var history = await _repository.GetByDateRangeAsync(fromDate, toDate, companyId, skip, pageSize);
        var totalCount = await _repository.CountByDateRangeAsync(fromDate, toDate, companyId);

        var dtos = _mapper.Map<IEnumerable<SubscriptionHistoryDto>>(history);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    public async Task LogSubscriptionEventAsync(
        Guid companyId,
        SubscriptionHistoryActionType actionType,
        object? oldValue = null,
        object? newValue = null,
        Guid? performedBy = null,
        string? notes = null)
    {
        var history = new SubscriptionHistory
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ActionType = actionType,
            OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
            NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow,
            Notes = notes,
            IsActive = true,
            IsDeleted = false
        };

        await _repository.AddAsync(history);
        await _unitOfWork.SaveChangesAsync();
    }
}


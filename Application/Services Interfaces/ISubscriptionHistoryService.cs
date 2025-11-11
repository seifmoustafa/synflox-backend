using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.SubscriptionHistory;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing subscription history records.
/// </summary>
public interface ISubscriptionHistoryService
{
    /// <summary>
    /// Gets the subscription history for a specific company.
    /// </summary>
    Task<(IEnumerable<SubscriptionHistoryDto> History, PaginationMetadata Meta)> GetHistoryByCompanyIdAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets subscription history within a date range.
    /// </summary>
    Task<(IEnumerable<SubscriptionHistoryDto> History, PaginationMetadata Meta)> GetHistoryByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? companyId = null,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Logs a subscription event to the history.
    /// </summary>
    Task LogSubscriptionEventAsync(
        Guid companyId,
        Domain.Enums.SubscriptionHistoryActionType actionType,
        object? oldValue = null,
        object? newValue = null,
        Guid? performedBy = null,
        string? notes = null);
}


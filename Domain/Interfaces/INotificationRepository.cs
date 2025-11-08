using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Notifications;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for Notification entity operations.
/// </summary>
public interface INotificationRepository : IBaseRepository<Guid, Notification>
{
    /// <summary>
    /// Gets notifications for a specific company.
    /// </summary>
    Task<IEnumerable<Notification>> GetByCompanyIdAsync(
        Guid companyId,
        bool? unreadOnly = null,
        int skip = 0,
        int take = 10);

    /// <summary>
    /// Gets all notifications with optional filters.
    /// </summary>
    Task<IEnumerable<Notification>> GetAllWithFiltersAsync(
        Guid? companyId = null,
        bool? unreadOnly = null,
        int skip = 0,
        int take = 10);

    /// <summary>
    /// Counts notifications for a company.
    /// </summary>
    Task<int> CountByCompanyIdAsync(Guid companyId, bool? unreadOnly = null);

    /// <summary>
    /// Counts all notifications with optional filters.
    /// </summary>
    Task<int> CountWithFiltersAsync(Guid? companyId = null, bool? unreadOnly = null);
}


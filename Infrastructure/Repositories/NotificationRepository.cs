using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Notifications;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class NotificationRepository : BaseRepository<Guid, Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetByCompanyIdAsync(
        Guid companyId,
        bool? unreadOnly = null,
        int skip = 0,
        int take = 10)
    {
        var query = _dbSet
            .Where(n => n.CompanyId == companyId && !n.IsDeleted);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetAllWithFiltersAsync(
        Guid? companyId = null,
        bool? unreadOnly = null,
        int skip = 0,
        int take = 10)
    {
        var query = _dbSet.Where(n => !n.IsDeleted);

        if (companyId.HasValue)
        {
            query = query.Where(n => n.CompanyId == companyId.Value);
        }

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, bool? unreadOnly = null)
    {
        var query = _dbSet
            .Where(n => n.CompanyId == companyId && !n.IsDeleted);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.CountAsync();
    }

    public async Task<int> CountWithFiltersAsync(Guid? companyId = null, bool? unreadOnly = null)
    {
        var query = _dbSet.Where(n => !n.IsDeleted);

        if (companyId.HasValue)
        {
            query = query.Where(n => n.CompanyId == companyId.Value);
        }

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.CountAsync();
    }
}


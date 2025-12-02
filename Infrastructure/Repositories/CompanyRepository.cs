using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CompanyRepository : BaseRepository<Guid, Company>, ICompanyRepository
    {
        public CompanyRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<Company?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Name == name && !c.IsDeleted);
        }

        #region Delete Cascade Support

        public async Task<int> GetSubscriptionsCountAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            return await _context.Subscriptions
                .CountAsync(s => s.CompanyId == companyId && !s.IsDeleted, cancellationToken);
        }

        public async Task<List<string>> GetSubscriptionNamesAsync(Guid companyId, int take = 10, CancellationToken cancellationToken = default)
        {
            return await _context.Subscriptions
                .Where(s => s.CompanyId == companyId && !s.IsDeleted)
                .Select(s => s.Plan.Name + " (" + (s.IsActive ? "Active" : "Inactive") + ")")
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetClientTokensCountAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            return await _context.ClientAccessTokens
                .CountAsync(t => t.CompanyId == companyId && !t.IsDeleted, cancellationToken);
        }

        public async Task<bool> HasRelatedRecordsAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            var hasSubscriptions = await _context.Subscriptions
                .AnyAsync(s => s.CompanyId == companyId && !s.IsDeleted, cancellationToken);
            if (hasSubscriptions) return true;

            var hasTokens = await _context.ClientAccessTokens
                .AnyAsync(t => t.CompanyId == companyId && !t.IsDeleted, cancellationToken);
            return hasTokens;
        }

        public async Task SoftDeleteSubscriptionsAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _context.Subscriptions
                .Where(s => s.CompanyId == companyId && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var sub in subscriptions)
            {
                sub.IsDeleted = true;
                sub.IsActive = false;
                sub.DeletedTimestamp = now;
                sub.UpdatedTimestamp = now;
            }
            
            // Also soft delete subscription histories
            var subIds = subscriptions.Select(s => s.Id).ToList();
            var histories = await _context.SubscriptionHistories
                .Where(h => subIds.Contains(h.SubscriptionId) && !h.IsDeleted)
                .ToListAsync(cancellationToken);
            
            foreach (var history in histories)
            {
                history.IsDeleted = true;
                history.DeletedTimestamp = now;
                history.UpdatedTimestamp = now;
            }
        }

        public async Task SoftDeleteClientTokensAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            var tokens = await _context.ClientAccessTokens
                .Where(t => t.CompanyId == companyId && !t.IsDeleted)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.IsDeleted = true;
                token.IsActive = false;
                token.DeletedTimestamp = now;
                token.UpdatedTimestamp = now;
            }
        }

        #endregion
    }
}


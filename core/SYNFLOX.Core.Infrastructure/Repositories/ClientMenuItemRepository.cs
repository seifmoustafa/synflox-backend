using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Navigation;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ClientMenuItemRepository : BaseRepository<Guid, ClientMenuItem>, IClientMenuItemRepository
    {
        public ClientMenuItemRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ClientMenuItem>> GetActiveMenuItemsWithChildrenAsync()
        {
            // Load all active menu items with their relationships
            // EF Core doesn't support recursive includes, so we load all items
            // and build the hierarchy in the service layer
            var allItems = await _dbSet
                .Where(m => !m.IsDeleted && m.IsActive)
                .Include(m => m.Parent)
                .OrderBy(m => m.Order)
                .AsNoTracking()
                .ToListAsync();

            // Build parent-child relationships
            foreach (var item in allItems)
            {
                if (item.ParentId.HasValue)
                {
                    item.Parent = allItems.FirstOrDefault(p => p.Id == item.ParentId.Value);
                }
                item.Children = allItems
                    .Where(c => c.ParentId == item.Id)
                    .OrderBy(c => c.Order)
                    .ToList();
            }

            // Return only top-level items (those without parents)
            return allItems.Where(m => m.ParentId == null);
        }
    }
}

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
    public class MenuItemsRepository : BaseRepository<Guid, MenuItems>, IMenuItemsRepository
    {
        public MenuItemsRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MenuItems>> GetActiveMenuItemssWithChildrenAsync()
        {
            // Load all active menu items with their relationships
            // EF Core doesn't support recursive includes, so we load all items
            // and build the hierarchy in the service layer
            var allItems = await _dbSet
                .Where(m => !m.IsDeleted && m.IsActive)
                .Include(m => m.ParentMenuItems)
                .OrderBy(m => m.Order)
                .AsNoTracking()
                .ToListAsync();

            // Build parent-child relationships
            foreach (var item in allItems)
            {
                if (item.ParentMenuItemsId.HasValue)
                {
                    item.ParentMenuItems = allItems.FirstOrDefault(p => p.Id == item.ParentMenuItemsId.Value);
                }
                item.Children = allItems
                    .Where(c => c.ParentMenuItemsId == item.Id)
                    .OrderBy(c => c.Order)
                    .ToList();
            }

            // Return only top-level items (those without parents)
            return allItems.Where(m => m.ParentMenuItemsId == null);
        }
    }
}


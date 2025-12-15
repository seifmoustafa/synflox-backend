using System;
using System.Threading.Tasks;
using Domain.Entities.Navigation;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository interface for AdminMenuItem entity operations.
    /// </summary>
    public interface IAdminMenuItemRepository : IBaseRepository<Guid, AdminMenuItem>
    {
        /// <summary>
        /// Gets all active admin menu items with their children loaded, ordered by Order field.
        /// </summary>
        Task<IEnumerable<AdminMenuItem>> GetActiveMenuItemsWithChildrenAsync();
    }
}

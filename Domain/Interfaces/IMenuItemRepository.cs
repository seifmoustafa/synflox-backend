using System;
using System.Threading.Tasks;
using Domain.Entities.Navigation;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository interface for MenuItems entity operations.
    /// </summary>
    public interface IMenuItemsRepository : IBaseRepository<Guid, MenuItems>
    {
        /// <summary>
        /// Gets all active menu items with their children loaded, ordered by Order field.
        /// </summary>
        Task<IEnumerable<MenuItems>> GetActiveMenuItemssWithChildrenAsync();
    }
}


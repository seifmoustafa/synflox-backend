using System;
using System.Threading.Tasks;
using Domain.Entities.Navigation;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository interface for ClientMenuItem entity operations.
    /// </summary>
    public interface IClientMenuItemRepository : IBaseRepository<Guid, ClientMenuItem>
    {
        /// <summary>
        /// Gets all active client menu items with their children loaded, ordered by Order field.
        /// </summary>
        Task<IEnumerable<ClientMenuItem>> GetActiveMenuItemsWithChildrenAsync();
    }
}

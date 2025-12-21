using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.MenuItems;

namespace Application.Services
{
    /// <summary>
    /// Service interface for managing admin portal menu items.
    /// </summary>
    public interface IAdminMenuItemService
    {
        /// <summary>
        /// Gets all active admin menu items with their children, ordered by Order field.
        /// Returns menu items and allowed pages array for navigation.
        /// </summary>
        Task<AdminMenuItemsResponseDto> GetMenuItemsAsync();

        /// <summary>
        /// Gets a menu item by ID.
        /// </summary>
        Task<AdminMenuItemDto?> GetMenuItemByIdAsync(GetMenuItemByIdRequest request);

        /// <summary>
        /// Creates a new menu item.
        /// </summary>
        Task<AdminMenuItemDto> CreateMenuItemAsync(CreateAdminMenuItemDto dto);

        /// <summary>
        /// Updates an existing menu item.
        /// </summary>
        Task<AdminMenuItemDto?> UpdateMenuItemAsync(UpdateMenuItemByIdRequest request);

        /// <summary>
        /// Deletes a menu item (soft delete).
        /// </summary>
        Task<bool> DeleteMenuItemAsync(DeleteMenuItemRequest request);
    }

    /// <summary>
    /// Service interface for managing client portal menu items.
    /// </summary>
    public interface IClientMenuItemService
    {
        /// <summary>
        /// Gets all active client menu items with their children, ordered by Order field.
        /// Returns menu items and allowed pages array for navigation.
        /// </summary>
        Task<ClientMenuItemsResponseDto> GetMenuItemsAsync();

        /// <summary>
        /// Gets a menu item by ID.
        /// </summary>
        Task<ClientMenuItemDto?> GetMenuItemByIdAsync(Guid id);
    }
}

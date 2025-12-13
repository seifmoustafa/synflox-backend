using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.MenuItems;

namespace Application.Services
{
    /// <summary>
    /// Service interface for managing menu items.
    /// </summary>
    public interface IMenuItemsService
    {
        /// <summary>
        /// Gets all active menu items with their children, ordered by Order field.
        /// Returns menu items and allowed pages array for navigation.
        /// </summary>
        Task<MenuItemssResponseDto> GetMenuItemssAsync();

        /// <summary>
        /// Gets a menu item by ID.
        /// </summary>
        Task<MenuItemsDto?> GetMenuItemsByIdAsync(GetMenuItemByIdRequest request);

        /// <summary>
        /// Creates a new menu item.
        /// </summary>
        Task<MenuItemsDto> CreateMenuItemsAsync(CreateMenuItemsDto dto);

        /// <summary>
        /// Updates an existing menu item.
        /// </summary>
        Task<MenuItemsDto?> UpdateMenuItemsAsync(UpdateMenuItemByIdRequest request);

        /// <summary>
        /// Deletes a menu item (soft delete).
        /// </summary>
        Task<bool> DeleteMenuItemsAsync(DeleteMenuItemRequest request);
    }
}


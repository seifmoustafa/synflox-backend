using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MenuItems
{
    /// <summary>
    /// DTO for creating a new menu item.
    /// </summary>
    public class CreateMenuItemsDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public required string Name { get; set; }

        [StringLength(500, ErrorMessage = "Href cannot exceed 500 characters")]
        public string? Href { get; set; }

        [Required(ErrorMessage = "Icon is required")]
        [StringLength(100, ErrorMessage = "Icon cannot exceed 100 characters")]
        public required string Icon { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Order must be a non-negative number")]
        public int Order { get; set; }

        /// <summary>
        /// Parent menu item ID (encrypted). Null for top-level items.
        /// </summary>
        public Guid? ParentMenuItemsId { get; set; }

        /// <summary>
        /// List of user types that can see this menu item.
        /// If null or empty, the menu item is visible to all authenticated users.
        /// Example: ["SuperAdmin", "Admin"] or ["SuperAdmin"]
        /// </summary>
        public List<string>? AllowedUserTypes { get; set; }

        [StringLength(100, ErrorMessage = "Notes cannot exceed 100 characters")]
        public string? Notes { get; set; }
    }
}


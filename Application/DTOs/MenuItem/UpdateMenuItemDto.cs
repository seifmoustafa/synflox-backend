using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MenuItems
{
    /// <summary>
    /// DTO for updating an existing menu item.
    /// </summary>
    public class UpdateMenuItemsDto
    {
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Href cannot exceed 500 characters")]
        public string? Href { get; set; }

        [StringLength(100, ErrorMessage = "Icon cannot exceed 100 characters")]
        public string? Icon { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Order must be a non-negative number")]
        public int? Order { get; set; }

        /// <summary>
        /// Parent menu item ID (encrypted). Null to remove parent relationship.
        /// </summary>
        public string? ParentMenuItemsId { get; set; }

        /// <summary>
        /// List of user types that can see this menu item.
        /// If null or empty, the menu item is visible to all authenticated users.
        /// Example: ["SuperAdmin", "Admin"] or ["SuperAdmin"]
        /// </summary>
        public List<string>? AllowedUserTypes { get; set; }

        public bool? IsActive { get; set; }

        [StringLength(100, ErrorMessage = "Notes cannot exceed 100 characters")]
        public string? Notes { get; set; }
    }
}


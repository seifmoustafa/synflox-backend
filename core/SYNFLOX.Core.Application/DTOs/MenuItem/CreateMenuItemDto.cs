using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MenuItems
{
    /// <summary>
    /// DTO for creating a new admin menu item.
    /// </summary>
    public class CreateAdminMenuItemDto
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
        public Guid? ParentId { get; set; }

        /// <summary>
        /// List of admin types that can see this menu item.
        /// If null or empty, visible to all authenticated admins.
        /// Example: ["SuperAdmin", "Admin"] or ["SuperAdmin"]
        /// </summary>
        public List<string>? AllowedUserTypes { get; set; }
    }

    /// <summary>
    /// DTO for creating a new client menu item.
    /// </summary>
    public class CreateClientMenuItemDto
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
        public Guid? ParentId { get; set; }

        /// <summary>
        /// List of permissions required to see this menu item.
        /// Example: ["CanManageDevices"] or ["CanViewSubscriptions"]
        /// </summary>
        public List<string>? RequiredPermissions { get; set; }
    }
}


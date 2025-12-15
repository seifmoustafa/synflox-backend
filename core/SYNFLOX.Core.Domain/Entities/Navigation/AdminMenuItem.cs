using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Navigation
{
    /// <summary>
    /// Represents a menu item in the admin portal navigation system.
    /// Supports hierarchical menu structure with parent-child relationships.
    /// This is separate from ClientMenuItem to allow independent menu configurations.
    /// </summary>
    public class AdminMenuItem : AuditEntity<Guid>
    {
        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

        /// <summary>
        /// Route path (e.g., "/dashboard", "/companies").
        /// Null for parent-only items that don't have a direct route.
        /// </summary>
        [StringLength(500)]
        public string? Href { get; set; }

        /// <summary>
        /// Icon name that matches keys in the frontend iconMap.
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string Icon { get; set; }

        /// <summary>
        /// Display order (lower = first).
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Parent menu item ID (for nested menu items).
        /// Null for top-level items.
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Navigation property for parent menu item.
        /// </summary>
        public AdminMenuItem? Parent { get; set; }

        /// <summary>
        /// Navigation property for child menu items.
        /// </summary>
        public ICollection<AdminMenuItem> Children { get; set; } = new List<AdminMenuItem>();

        /// <summary>
        /// JSON array of admin types that can see this menu item.
        /// Example: ["SuperAdmin", "Admin"] or ["SuperAdmin"] or null (visible to all).
        /// If null or empty, the menu item is visible to all authenticated admins.
        /// </summary>
        [StringLength(500)]
        public string? AllowedUserTypes { get; set; }
    }
}

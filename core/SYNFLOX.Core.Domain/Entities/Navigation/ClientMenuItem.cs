using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Navigation
{
    /// <summary>
    /// Represents a menu item in the client portal navigation system.
    /// Supports hierarchical menu structure with parent-child relationships.
    /// This is separate from AdminMenuItem to allow independent menu configurations.
    /// </summary>
    public class ClientMenuItem : AuditEntity<Guid>
    {
        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

        /// <summary>
        /// Route path (e.g., "/dashboard", "/devices", "/subscription").
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
        public ClientMenuItem? Parent { get; set; }

        /// <summary>
        /// Navigation property for child menu items.
        /// </summary>
        public ICollection<ClientMenuItem> Children { get; set; } = new List<ClientMenuItem>();

        /// <summary>
        /// JSON array of required permissions to see this menu item.
        /// Based on CompanyAdmin permission flags: CanViewSubscriptions, CanManageDevices, etc.
        /// Example: ["CanManageDevices"] or ["CanViewSubscriptions", "CanViewBilling"]
        /// If null or empty, visible to all authenticated company admins.
        /// </summary>
        [StringLength(500)]
        public string? RequiredPermissions { get; set; }
    }
}

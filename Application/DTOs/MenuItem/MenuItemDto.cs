using System;
using System.Collections.Generic;

namespace Application.DTOs.MenuItems
{
    /// <summary>
    /// Menu item response DTO.
    /// </summary>
    public class MenuItemsDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Href { get; set; }
        public string Icon { get; set; } = string.Empty;
        public int Order { get; set; }
        public MenuItemsReferenceDto? ParentMenuItems { get; set; }
        public List<MenuItemsDto> Children { get; set; } = new List<MenuItemsDto>();
        public List<string> AllowedUserTypes { get; set; } = new List<string>();
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public DateTime? UpdatedTimestamp { get; set; }
        public DateTime? DeletedTimestamp { get; set; }
    }

    /// <summary>
    /// Reference DTO for parent menu item (used in nested structures).
    /// </summary>
    public class MenuItemsReferenceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}


using System;
using System.Collections.Generic;

namespace Application.DTOs.MenuItems
{
    /// <summary>
    /// Admin menu item response DTO.
    /// </summary>
    public class AdminMenuItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Href { get; set; }
        public string Icon { get; set; } = string.Empty;
        public int Order { get; set; }
        public AdminMenuItemReferenceDto? Parent { get; set; }
        public List<AdminMenuItemDto> Children { get; set; } = new List<AdminMenuItemDto>();
        public List<string> AllowedUserTypes { get; set; } = new List<string>();
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public DateTime? UpdatedTimestamp { get; set; }
        public DateTime? DeletedTimestamp { get; set; }
    }

    /// <summary>
    /// Reference DTO for parent admin menu item.
    /// </summary>
    public class AdminMenuItemReferenceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Client menu item response DTO.
    /// </summary>
    public class ClientMenuItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Href { get; set; }
        public string Icon { get; set; } = string.Empty;
        public int Order { get; set; }
        public ClientMenuItemReferenceDto? Parent { get; set; }
        public List<ClientMenuItemDto> Children { get; set; } = new List<ClientMenuItemDto>();
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Reference DTO for parent client menu item.
    /// </summary>
    public class ClientMenuItemReferenceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}


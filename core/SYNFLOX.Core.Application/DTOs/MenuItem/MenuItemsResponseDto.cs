using System.Collections.Generic;

namespace Application.DTOs.MenuItems
{
    /// <summary>
    /// Response DTO for admin menu items endpoint.
    /// Contains menu items and allowed pages array.
    /// </summary>
    public class AdminMenuItemsResponseDto
    {
        public List<AdminMenuItemDto> MenuItems { get; set; } = new List<AdminMenuItemDto>();
        public List<string> Pages { get; set; } = new List<string>();
    }

    /// <summary>
    /// Response DTO for client menu items endpoint.
    /// Contains menu items and allowed pages array.
    /// </summary>
    public class ClientMenuItemsResponseDto
    {
        public List<ClientMenuItemDto> MenuItems { get; set; } = new List<ClientMenuItemDto>();
        public List<string> Pages { get; set; } = new List<string>();
    }
}


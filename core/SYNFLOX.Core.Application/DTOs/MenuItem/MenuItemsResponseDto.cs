using System.Collections.Generic;

namespace Application.DTOs.MenuItems
{
    /// <summary>
    /// Response DTO for the menu items endpoint.
    /// Contains menu items and allowed pages array.
    /// </summary>
    public class MenuItemssResponseDto
    {
        public List<MenuItemsDto> MenuItems { get; set; } = new List<MenuItemsDto>();
        public List<string> Pages { get; set; } = new List<string>();
    }
}


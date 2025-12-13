using System;

namespace Application.DTOs.MenuItems;

public class UpdateMenuItemByIdRequest
{
    public Guid MenuItemId { get; set; }
    public UpdateMenuItemsDto UpdateData { get; set; } = new();
}

using System;

namespace Application.DTOs.MenuItems;

public class UpdateMenuItemByIdRequest
{
    public Guid MenuItemId { get; set; }
    public UpdateAdminMenuItemDto UpdateData { get; set; } = new();
}

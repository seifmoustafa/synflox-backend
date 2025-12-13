using System;

namespace Application.DTOs.AdminType;

public class UpdateAdminTypeByIdRequest
{
    public Guid AdminTypeId { get; set; }
    public UpdateAdminTypeDto UpdateData { get; set; } = new();
}

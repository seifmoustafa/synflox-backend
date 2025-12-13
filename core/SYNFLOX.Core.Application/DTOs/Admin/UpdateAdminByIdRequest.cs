using System;

namespace Application.DTOs.Admin;

public class UpdateAdminByIdRequest
{
    public Guid AdminId { get; set; }
    public UpdateAdminRequest UpdateData { get; set; } = new();
}

using System;

namespace Application.DTOs.Admin;

public class ChangePasswordByIdRequest
{
    public Guid AdminId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}

using System;
using Domain.Enums;

namespace Application.DTOs.User;

public class UpdateUserBase : UserEditableBase
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

using System;
using Domain.Enums;

namespace Application.DTOs.User;

public class UpdateUserDto : UpdateUserBase
{
    public Guid Id { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }
}

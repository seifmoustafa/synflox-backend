using System;

namespace Application.DTOs.Admin;

public class AdminDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AdminTypeName { get; set; }
}

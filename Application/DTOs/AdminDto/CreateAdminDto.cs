using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Admin;

public class CreateAdminDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public Guid AdminTypeId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

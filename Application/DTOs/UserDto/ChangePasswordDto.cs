using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.User;

public class ChangePasswordDto
{
    public Guid Id { get; set; }

    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}

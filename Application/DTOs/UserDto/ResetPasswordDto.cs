using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.User;

public class ResetPasswordDto
{
    public Guid Id { get; set; }

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}

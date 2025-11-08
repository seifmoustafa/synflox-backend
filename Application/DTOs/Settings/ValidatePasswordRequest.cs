using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Settings;

public class ValidatePasswordRequest
{
    [Required(ErrorMessage = "Password is required")]
    public required string Password { get; set; }

    public Guid? AdminId { get; set; } // Optional, for checking password reuse
}


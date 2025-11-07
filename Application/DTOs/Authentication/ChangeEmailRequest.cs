using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication;

public class ChangeEmailRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

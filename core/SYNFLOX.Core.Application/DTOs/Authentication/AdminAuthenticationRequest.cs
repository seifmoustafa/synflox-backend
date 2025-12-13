namespace Application.DTOs.Authentication;

using System.ComponentModel.DataAnnotations;

public class AdminAuthenticationRequest
{
    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication;

public class ChangeUsernameRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
}

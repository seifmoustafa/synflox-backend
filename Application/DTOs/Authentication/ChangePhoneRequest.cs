using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication;

public class ChangePhoneRequest
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
}

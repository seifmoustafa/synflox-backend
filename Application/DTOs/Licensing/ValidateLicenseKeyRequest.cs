using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

public class ValidateLicenseKeyRequest
{
    [Required(ErrorMessage = "License key is required")]
    public string LicenseKey { get; set; } = string.Empty;
}


using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

/// <summary>
/// Request to validate an offline license key
/// </summary>
public class ValidateLicenseKeyRequest
{
    /// <summary>
    /// The encrypted license key string
    /// </summary>
    [Required(ErrorMessage = "License key is required")]
    public string LicenseKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to perform online validation (check DB for subscription status)
    /// Set to false for pure offline systems
    /// </summary>
    public bool ValidateOnline { get; set; } = false;
}


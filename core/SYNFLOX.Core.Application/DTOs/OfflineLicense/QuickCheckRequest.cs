using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// Request for quick license key validation check.
/// Uses POST to avoid exposing license key in URLs.
/// </summary>
public class QuickCheckRequest
{
    /// <summary>
    /// The license key to validate
    /// </summary>
    [Required]
    public string LicenseKey { get; set; } = string.Empty;
}

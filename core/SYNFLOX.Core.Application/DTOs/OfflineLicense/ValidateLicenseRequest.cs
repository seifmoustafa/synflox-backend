using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// Request to validate an offline license key.
/// Can be used by client applications or for admin verification.
/// </summary>
public class ValidateLicenseRequest
{
    /// <summary>
    /// The license key to validate (Base64Url encoded)
    /// Max size prevents DoS via huge payloads
    /// </summary>
    [Required(ErrorMessage = "License key is required")]
    [StringLength(10000, MinimumLength = 100, ErrorMessage = "License key must be between 100 and 10000 characters")]
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// Machine fingerprint of the requesting machine
    /// Required if license is machine-bound
    /// </summary>
    public MachineFingerprint? MachineFingerprint { get; set; }

    /// <summary>
    /// Whether to perform online validation (check subscription status in DB)
    /// Set to false for pure offline validation
    /// </summary>
    public bool ValidateOnline { get; set; } = false;

    /// <summary>
    /// Whether to update the last validation timestamp
    /// Only set to true for actual client validations, not admin checks
    /// </summary>
    public bool UpdateLastValidation { get; set; } = false;

    /// <summary>
    /// Current system time from client (for clock drift detection)
    /// </summary>
    public long? ClientTimestamp { get; set; }
}

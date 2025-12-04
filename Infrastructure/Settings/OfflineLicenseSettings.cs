using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

/// <summary>
/// Configuration settings for offline license key generation and validation.
/// Uses AES-256-GCM for authenticated encryption.
/// </summary>
public class OfflineLicenseSettings
{
    /// <summary>
    /// AES-256 encryption key (32 bytes, base64 encoded = 44 characters).
    /// Used for encrypting license payload.
    /// CRITICAL: Keep this secret and rotate periodically.
    /// </summary>
    [Required]
    [StringLength(44, MinimumLength = 44)]
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Secondary signing key for additional integrity verification (32 bytes, base64 encoded).
    /// Used in payload checksum calculation.
    /// </summary>
    [Required]
    [StringLength(44, MinimumLength = 44)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Salt for machine fingerprint hashing (16+ bytes, base64 encoded).
    /// Makes fingerprint harder to reverse-engineer.
    /// </summary>
    [Required]
    [MinLength(16)]
    public string FingerprintSalt { get; set; } = string.Empty;

    /// <summary>
    /// Current license key format version.
    /// Increment when making breaking changes to key format.
    /// </summary>
    [Range(1, 999)]
    public int CurrentVersion { get; set; } = 1;

    /// <summary>
    /// Minimum supported version for validation.
    /// Keys below this version will be rejected.
    /// </summary>
    [Range(1, 999)]
    public int MinimumSupportedVersion { get; set; } = 1;

    /// <summary>
    /// Maximum number of machines allowed per license (0 = unlimited).
    /// </summary>
    [Range(0, 100)]
    public int MaxMachinesPerLicense { get; set; } = 1;

    /// <summary>
    /// Days before expiry to start showing warnings to users.
    /// </summary>
    [Range(1, 90)]
    public int ExpiryWarningDays { get; set; } = 30;

    /// <summary>
    /// Whether to enforce machine fingerprint binding.
    /// Set to false for development/testing.
    /// </summary>
    public bool EnforceMachineBinding { get; set; } = true;

    /// <summary>
    /// Whether to enable clock tampering detection.
    /// </summary>
    public bool EnableClockTamperDetection { get; set; } = true;

    /// <summary>
    /// Maximum allowed clock drift in hours before flagging as tampered.
    /// Accounts for timezone changes and minor adjustments.
    /// </summary>
    [Range(1, 72)]
    public int MaxClockDriftHours { get; set; } = 24;

    /// <summary>
    /// Issuer identifier embedded in license keys.
    /// Used to verify keys came from this system.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Issuer { get; set; } = "SYNFLOX";
}

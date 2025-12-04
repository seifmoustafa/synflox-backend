using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.OfflineLicense;

/// <summary>
/// Machine fingerprint data collected from client for hardware binding.
/// Multiple identifiers are combined and hashed for unique machine identification.
/// All fields have max length limits to prevent DoS attacks.
/// </summary>
public class MachineFingerprint
{
    /// <summary>
    /// CPU processor ID
    /// </summary>
    [StringLength(200)]
    public string? CpuId { get; set; }

    /// <summary>
    /// Motherboard/baseboard serial number
    /// </summary>
    [StringLength(200)]
    public string? MotherboardSerial { get; set; }

    /// <summary>
    /// Primary disk drive serial number
    /// </summary>
    [StringLength(200)]
    public string? DiskSerial { get; set; }

    /// <summary>
    /// Primary MAC address (network adapter)
    /// </summary>
    [StringLength(50)]
    public string? MacAddress { get; set; }

    /// <summary>
    /// BIOS UUID
    /// </summary>
    [StringLength(100)]
    public string? BiosUuid { get; set; }

    /// <summary>
    /// Windows product ID or Linux machine-id
    /// </summary>
    [StringLength(200)]
    public string? OsProductId { get; set; }

    /// <summary>
    /// Machine hostname (additional identifier)
    /// </summary>
    [StringLength(255)]
    public string? Hostname { get; set; }

    /// <summary>
    /// Pre-computed fingerprint hash from client (for display purposes only).
    /// SECURITY WARNING: This value is NEVER used for validation - server always
    /// recomputes the hash from raw identifiers to prevent spoofing.
    /// </summary>
    public string? PrecomputedHash { get; set; }

    /// <summary>
    /// Validates that minimum required identifiers are present.
    /// SECURITY: PrecomputedHash is NOT counted - actual hardware identifiers are required.
    /// </summary>
    public bool HasMinimumIdentifiers()
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(CpuId)) count++;
        if (!string.IsNullOrWhiteSpace(MotherboardSerial)) count++;
        if (!string.IsNullOrWhiteSpace(DiskSerial)) count++;
        if (!string.IsNullOrWhiteSpace(MacAddress)) count++;
        if (!string.IsNullOrWhiteSpace(BiosUuid)) count++;
        if (!string.IsNullOrWhiteSpace(OsProductId)) count++;

        // Require at least 2 hardware identifiers for reliable binding
        // NOTE: PrecomputedHash is intentionally NOT accepted as a bypass
        return count >= 2;
    }
}

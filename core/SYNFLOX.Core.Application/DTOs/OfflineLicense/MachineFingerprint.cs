using System.Collections.Generic;
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
    /// REQUIRED: MAC Address AND Motherboard Serial (these are mandatory)
    /// OPTIONAL: CPU, Disk, BIOS, OS Product ID (additional security)
    /// SECURITY: PrecomputedHash is NEVER counted - actual hardware identifiers are required.
    /// </summary>
    public bool HasMinimumIdentifiers()
    {
        // MAC Address and Motherboard Serial are REQUIRED (mandatory minimum)
        // These are the most reliable hardware identifiers that:
        // - Are easy to get on any OS (Windows/Linux/Mac)
        // - Rarely change (unless hardware swap)
        // - Are unique enough for licensing purposes
        var hasMacAddress = !string.IsNullOrWhiteSpace(MacAddress);
        var hasMotherboardSerial = !string.IsNullOrWhiteSpace(MotherboardSerial);
        
        // Both MAC and Motherboard are REQUIRED
        return hasMacAddress && hasMotherboardSerial;
    }
    
    /// <summary>
    /// Gets the count of optional identifiers provided (for additional security scoring).
    /// Higher count = more secure binding.
    /// </summary>
    public int GetOptionalIdentifierCount()
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(CpuId)) count++;
        if (!string.IsNullOrWhiteSpace(DiskSerial)) count++;
        if (!string.IsNullOrWhiteSpace(BiosUuid)) count++;
        if (!string.IsNullOrWhiteSpace(OsProductId)) count++;
        return count;
    }
    
    /// <summary>
    /// Gets missing required identifiers for error messaging.
    /// </summary>
    public List<string> GetMissingRequiredIdentifiers()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(MacAddress)) missing.Add("MAC Address");
        if (string.IsNullOrWhiteSpace(MotherboardSerial)) missing.Add("Motherboard Serial");
        return missing;
    }
}

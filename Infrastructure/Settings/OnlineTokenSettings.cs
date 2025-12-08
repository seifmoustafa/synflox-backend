namespace Infrastructure.Settings;

/// <summary>
/// Settings for Online Client JWT tokens.
/// </summary>
public class OnlineTokenSettings
{
    public const string SectionName = "OnlineTokenSettings";
    
    /// <summary>
    /// Secret key for signing tokens (should be at least 256 bits).
    /// </summary>
    public required string SecretKey { get; set; }
    
    /// <summary>
    /// Token issuer (e.g., "SYNFLOX").
    /// </summary>
    public required string Issuer { get; set; }
    
    /// <summary>
    /// Token audience (e.g., "synflox-online-clients").
    /// </summary>
    public required string Audience { get; set; }
    
    /// <summary>
    /// Default token expiry in days.
    /// </summary>
    public int DefaultExpiryDays { get; set; } = 365;
    
    /// <summary>
    /// Maximum allowed expiry days.
    /// </summary>
    public int MaxExpiryDays { get; set; } = 730; // 2 years
    
    /// <summary>
    /// Whether to allow unlimited device binding.
    /// </summary>
    public bool AllowUnlimitedDevices { get; set; } = false;
    
    /// <summary>
    /// Default max devices if not specified by plan.
    /// </summary>
    public int DefaultMaxDevices { get; set; } = 10;
}

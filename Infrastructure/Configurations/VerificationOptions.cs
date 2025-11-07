namespace Infrastructure.Configurations;

public class VerificationOptions
{
    /// <summary>
    /// Lifetime of generated OTP codes in minutes.
    /// </summary>
    public int CodeExpiryMinutes { get; set; } = 10;

    /// <summary>
    /// Window in seconds for counting OTP resend attempts.
    /// </summary>
    public int ResendWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum allowed resends within the window.
    /// </summary>
    public int MaxResendsPerWindow { get; set; } = 3;
}

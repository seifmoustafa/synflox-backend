namespace Infrastructure.Configurations;

/// <summary>
/// Configuration for sending SMS messages using Twilio.
/// Messages are logged when <see cref="Mode"/> is set to <c>Dev</c>.
/// </summary>
public class SmsSettings
{
    /// <summary>Sender phone number or identifier.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Twilio account identifier.</summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>Twilio authentication token.</summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>Controls which sender implementation to use.</summary>
    public string Mode { get; set; } = "Dev";
}

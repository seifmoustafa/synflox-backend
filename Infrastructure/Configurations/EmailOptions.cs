using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configurations;

/// <summary>
/// Settings for the SMTP server used to send OTP messages.
/// Credentials (<c>User</c> and <c>Pass</c>) are expected from environment
/// variables or user-secrets and therefore are not stored in the repository.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// SMTP host name. Defaults to Gmail.
    /// </summary>
    [Required]
    public string Host { get; set; } = "smtp.gmail.com";

    /// <summary>
    /// SMTP port number.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP username loaded from configuration.
    /// </summary>
    [Required]
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password loaded from configuration.
    /// </summary>
    [Required]
    public string Pass { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the sender.
    /// </summary>
    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Email address that appears in the From header.
    /// </summary>
    [Required, EmailAddress]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Determines which sender implementation is used (Dev/Test/Prod).
    /// </summary>
    public string Mode { get; set; } = "Prod";
}

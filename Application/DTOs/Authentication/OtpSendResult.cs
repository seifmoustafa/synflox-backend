namespace Application.DTOs.Authentication;

/// <summary>
/// Result of attempting to send an OTP.
/// </summary>
public class OtpSendResult
{
    /// <summary>
    /// Whether the OTP was sent.
    /// </summary>
    public bool Sent { get; init; }

    /// <summary>
    /// If throttled, indicates how many seconds to wait before retrying.
    /// </summary>
    public int? RetryAfterSeconds { get; init; }

    /// <summary>
    /// Optional message describing the result.
    /// </summary>
    public string? Message { get; init; }
}

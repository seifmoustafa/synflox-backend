using System;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// DTO for client token usage log entries
/// </summary>
public class ClientTokenUsageLogDto
{
    /// <summary>
    /// Usage log entry ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Token ID that was used
    /// </summary>
    public Guid ClientTokenId { get; set; }

    /// <summary>
    /// API endpoint that was accessed
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method used
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// HTTP response status code
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// When the request was made
    /// </summary>
    public DateTime RequestTimestampUtc { get; set; }

    /// <summary>
    /// Whether the request was successful (2xx status code)
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional request metadata
    /// </summary>
    public string? RequestMetadata { get; set; }
}

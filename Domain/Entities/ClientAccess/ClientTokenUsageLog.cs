using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.ClientAccess;

/// <summary>
/// Audit log for client token usage
/// Tracks every API call made with client tokens for security and analytics
/// </summary>
public class ClientTokenUsageLog : BaseEntity<Guid>
{
    /// <summary>
    /// The client token that was used
    /// </summary>
    public Guid ClientTokenId { get; set; }

    /// <summary>
    /// The API endpoint that was called
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Endpoint { get; set; }

    /// <summary>
    /// HTTP method used (GET, POST, etc.)
    /// </summary>
    [Required]
    [StringLength(10)]
    public required string HttpMethod { get; set; }

    /// <summary>
    /// Client IP address
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    [StringLength(500)]
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
    /// When the API call was made (UTC)
    /// </summary>
    public DateTime RequestTimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Any error message if the request failed
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional request metadata (JSON)
    /// </summary>
    [StringLength(2000)]
    public string? RequestMetadata { get; set; }

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool IsSuccessful => ResponseStatusCode >= 200 && ResponseStatusCode < 300;

    // Navigation properties
    public ClientAccessToken ClientToken { get; set; } = null!;
}

using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Analytics;

/// <summary>
/// Represents a log entry for API usage by a company.
/// </summary>
public class CompanyUsageLog : BaseEntity<Guid>
{
    /// <summary>
    /// The company that made the API call.
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Navigation property to the company.
    /// </summary>
    public Domain.Entities.Licensing.Company Company { get; set; } = null!;

    /// <summary>
    /// The API endpoint that was called.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string Endpoint { get; set; }

    /// <summary>
    /// The HTTP method (GET, POST, PUT, DELETE, etc.).
    /// </summary>
    [Required]
    [StringLength(10)]
    public required string Method { get; set; }

    /// <summary>
    /// Timestamp when the request was made.
    /// </summary>
    [Required]
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// IP address of the requester.
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the request.
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// API key ID if the request was authenticated with an API key.
    /// </summary>
    public Guid? ApiKeyId { get; set; }
}


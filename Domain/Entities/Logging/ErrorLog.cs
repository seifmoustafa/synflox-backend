using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Logging;

/// <summary>
/// Represents an error log entry with full context for debugging.
/// </summary>
public class ErrorLog : BaseEntity<Guid>
{
    /// <summary>
    /// Unique error ID for tracking.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string ErrorId { get; set; }

    /// <summary>
    /// Error message.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public required string Message { get; set; }

    /// <summary>
    /// Full stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Exception type name.
    /// </summary>
    [StringLength(500)]
    public string? ExceptionType { get; set; }

    /// <summary>
    /// HTTP method if error occurred during a request.
    /// </summary>
    [StringLength(10)]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request path if error occurred during a request.
    /// </summary>
    [StringLength(1000)]
    public string? RequestPath { get; set; }

    /// <summary>
    /// Query string if available.
    /// </summary>
    [StringLength(2000)]
    public string? QueryString { get; set; }

    /// <summary>
    /// Request body if available (truncated to 5000 chars).
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// HTTP status code if applicable.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// IP address of the requester.
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// User/Admin ID if authenticated.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Company ID if applicable.
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Additional context data as JSON.
    /// </summary>
    public string? ContextData { get; set; }

    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Severity level (Error, Warning, Critical).
    /// </summary>
    [Required]
    [StringLength(20)]
    public required string Severity { get; set; } = "Error";
}




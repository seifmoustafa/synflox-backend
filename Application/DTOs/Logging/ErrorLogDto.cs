using System;

namespace Application.DTOs.Logging;

/// <summary>
/// DTO for error log information.
/// </summary>
public class ErrorLogDto
{
    public Guid Id { get; set; }
    public string ErrorId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? ExceptionType { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestPath { get; set; }
    public string? QueryString { get; set; }
    public int? StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? ContextData { get; set; }
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
}




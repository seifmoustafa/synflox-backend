using System;

namespace Application.DTOs.Authentication;

/// <summary>
/// DTO for login attempt records.
/// </summary>
public class LoginAttemptDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; }
    public Guid? AdminId { get; set; }
}


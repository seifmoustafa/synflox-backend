using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Company;

/// <summary>
/// Request DTO for sending custom emails to companies
/// </summary>
public class CustomEmailRequest
{
    /// <summary>
    /// Company ID (encrypted) - REQUIRED for custom emails
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email title (appears in the email header)
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Main email message content (HTML supported)
    /// </summary>
    [Required]
    [StringLength(5000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Reason or cause for sending this email (optional)
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Additional notes (optional)
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Email priority level
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    /// <summary>
    /// Custom accent color for email template (hex color)
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid hex color format")]
    public string AccentColor { get; set; } = "#2563eb";

    /// <summary>
    /// Whether to include company branding
    /// </summary>
    public bool IncludeBranding { get; set; } = true;

    /// <summary>
    /// Whether to include standard footer
    /// </summary>
    public bool IncludeFooter { get; set; } = true;

    /// <summary>
    /// Use minimal template with only content (no SYNFLOX branding)
    /// </summary>
    public bool UseMinimalTemplate { get; set; } = false;
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Request DTO for sending bulk custom emails
/// </summary>
public class BulkCustomEmailRequest
{
    /// <summary>
    /// List of encrypted Company IDs - REQUIRED for bulk custom emails
    /// </summary>
    [Required]
    public List<Guid> CompanyIds { get; set; } = new();

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email title (appears in the email header)
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Main email message content (HTML supported)
    /// </summary>
    [Required]
    [StringLength(5000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Reason or cause for sending this email (optional)
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Email priority level
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    /// <summary>
    /// Custom accent color for email template (hex color)
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid hex color format")]
    public string AccentColor { get; set; } = "#2563eb";

    /// <summary>
    /// Whether to include SYNFLOX branding in email template
    /// </summary>
    public bool IncludeBranding { get; set; } = true;

    /// <summary>
    /// Whether to include standard footer
    /// </summary>
    public bool IncludeFooter { get; set; } = true;

    /// <summary>
    /// Custom header text (optional)
    /// </summary>
    [StringLength(200)]
    public string? CustomHeader { get; set; }

    /// <summary>
    /// Custom footer text (optional)
    /// </summary>
    [StringLength(500)]
    public string? CustomFooter { get; set; }

    /// <summary>
    /// Call-to-action button text (optional)
    /// </summary>
    [StringLength(50)]
    public string? ButtonText { get; set; }

    /// <summary>
    /// Call-to-action button URL (optional)
    /// </summary>
    [Url]
    [StringLength(500)]
    public string? ButtonUrl { get; set; }

    /// <summary>
    /// Custom CSS styles (optional)
    /// </summary>
    [StringLength(1000)]
    public string? CustomStyles { get; set; }

    /// <summary>
    /// Use minimal template with only content (no SYNFLOX branding)
    /// </summary>
    public bool UseMinimalTemplate { get; set; } = false;
}

/// <summary>
/// Response DTO for custom email operations
/// </summary>
public class CustomEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string? ErrorDetails { get; set; }
    public string? RecipientEmail { get; set; }
    public string? CompanyName { get; set; }
}

/// <summary>
/// Response DTO for bulk custom email operations
/// </summary>
public class BulkCustomEmailResponse
{
    public int TotalEmails { get; set; }
    public int SuccessfulEmails { get; set; }
    public int FailedEmails { get; set; }
    public List<CustomEmailResponse> Results { get; set; } = new();
    public bool AllSuccessful => FailedEmails == 0;
    public string Summary => $"Sent {SuccessfulEmails}/{TotalEmails} emails successfully";
}

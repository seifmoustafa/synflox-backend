using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Company;

/// <summary>
/// Request DTO for company actions (activate, deactivate)
/// </summary>
public class CompanyActionRequest
{
    /// <summary>
    /// Encrypted Company ID
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Reason for the action
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes (optional)
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether to send email notification
    /// </summary>
    public bool SendEmailNotification { get; set; } = true;
}

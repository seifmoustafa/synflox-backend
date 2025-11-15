using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.Services;
using Application.DTOs.Company;
using Application.DTOs.Responses;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for sending custom emails to companies
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class CustomEmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILocalizationService _localizer;

    public CustomEmailController(IEmailService emailService, ILocalizationService localizer)
    {
        _emailService = emailService;
        _localizer = localizer;
    }

    /// <summary>
    /// Send a custom email to a specific company or email address
    /// </summary>
    /// <param name="request">Custom email request</param>
    /// <param name="lang">Language for email content (en, ar)</param>
    /// <returns>Email send result</returns>
    [HttpPost("send")]
    public async Task<IActionResult> SendCustomEmail([FromBody] CustomEmailRequest request, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest("Request cannot be null");

        try
        {
            var result = await _emailService.SendCustomEmailAsync(request, lang);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<CustomEmailResponse>(200, "Email sent successfully", result));
            }
            else
            {
                return BadRequest(new ApiResponse<CustomEmailResponse>(400, result.Message, result));
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Send custom emails to multiple companies or email addresses
    /// </summary>
    /// <param name="request">Bulk custom email request</param>
    /// <param name="lang">Language for email content (en, ar)</param>
    /// <returns>Bulk email send results</returns>
    [HttpPost("send/bulk")]
    public async Task<IActionResult> SendBulkCustomEmail([FromBody] BulkCustomEmailRequest request, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest("Request cannot be null");

        try
        {
            var result = await _emailService.SendBulkCustomEmailAsync(request, lang);
            
            return Ok(new ApiResponse<BulkCustomEmailResponse>(200, result.Summary, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Send a quick notification email to a company
    /// </summary>
    /// <param name="companyId">Encrypted Company ID</param>
    /// <param name="subject">Email subject</param>
    /// <param name="message">Email message</param>
    /// <param name="lang">Language for email content (en, ar)</param>
    /// <returns>Email send result</returns>
    [HttpPost("notify/{companyId}")]
    public async Task<IActionResult> SendQuickNotification(
        Guid companyId, 
        [FromQuery] string subject, 
        [FromQuery] string message,
        [FromQuery] string? lang = null)
    {
        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
        {
            return BadRequest("Subject and message are required");
        }

        try
        {
            var request = new CustomEmailRequest
            {
                CompanyId = companyId,
                Subject = subject,
                Title = subject,
                Message = message,
                AccentColor = "#2563eb",
                IncludeBranding = true,
                IncludeFooter = true
            };

            var result = await _emailService.SendCustomEmailAsync(request, lang);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<CustomEmailResponse>(200, "Notification sent successfully", result));
            }
            else
            {
                return BadRequest(new ApiResponse<CustomEmailResponse>(400, result.Message, result));
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Send a maintenance notification to all companies
    /// </summary>
    /// <param name="subject">Maintenance subject</param>
    /// <param name="message">Maintenance message</param>
    /// <param name="scheduledTime">Scheduled maintenance time</param>
    /// <param name="lang">Language for email content (en, ar)</param>
    /// <returns>Bulk email send results</returns>
    [HttpPost("maintenance")]
    public async Task<IActionResult> SendMaintenanceNotification(
        [FromQuery] string subject,
        [FromQuery] string message,
        [FromQuery] DateTime? scheduledTime = null,
        [FromQuery] string? lang = null)
    {
        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
        {
            return BadRequest("Subject and message are required");
        }

        try
        {
            var maintenanceMessage = message;
            if (scheduledTime.HasValue)
            {
                maintenanceMessage += $@"<br><br>
                    <strong>🕐 Scheduled Time:</strong> {scheduledTime.Value:MMMM dd, yyyy 'at' HH:mm} UTC<br>
                    <strong>⏱️ Expected Duration:</strong> Approximately 2-4 hours<br>
                    <strong>📱 Status Updates:</strong> Follow our status page for real-time updates";
            }

            // TODO: Get all active companies from database
            // For now, this is a placeholder - you'll need to implement company fetching
            var request = new BulkCustomEmailRequest
            {
                CompanyIds = new List<Guid>(), // TODO: Populate with actual company IDs
                Subject = $"🔧 {subject}",
                Title = "🔧 Scheduled Maintenance Notice",
                Message = maintenanceMessage,
                AccentColor = "#f59e0b"
            };

            var result = await _emailService.SendBulkCustomEmailAsync(request, lang);
            
            return Ok(new ApiResponse<BulkCustomEmailResponse>(200, "Maintenance notifications sent", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}

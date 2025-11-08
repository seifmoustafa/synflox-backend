using System;
using System.Threading.Tasks;
using Application.DTOs.Notifications;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public NotificationController(
        INotificationService notificationService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _notificationService = notificationService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    /// <summary>
    /// Gets notifications for a specific company (for external systems to poll).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] Guid? companyId,
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            Guid? decryptedCompanyId = null;
            if (companyId.HasValue)
            {
                decryptedCompanyId = _idEncryption.Decrypt(companyId.Value);
            }

            var (notifications, meta) = await _notificationService.GetAllNotificationsAsync(
                decryptedCompanyId,
                unreadOnly,
                page,
                pageSize);

            return Ok(new ApiResponse<object>(200, string.Empty, new { notifications, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets notifications for a specific company by company ID.
    /// </summary>
    [HttpGet("company/{companyId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetCompanyNotifications(
        Guid companyId,
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var (notifications, meta) = await _notificationService.GetNotificationsByCompanyIdAsync(
                decryptedId,
                unreadOnly,
                page,
                pageSize);

            return Ok(new ApiResponse<object>(200, string.Empty, new { notifications, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets unread notification count for a company.
    /// </summary>
    [HttpGet("company/{companyId}/unread-count")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUnreadCount(Guid companyId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var count = await _notificationService.GetUnreadCountAsync(decryptedId);
            return Ok(new ApiResponse<object>(200, string.Empty, new { count }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    [HttpPut("{id}/read")]
    [AllowAnonymous]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            await _notificationService.MarkAsReadAsync(decryptedId);
            return Ok(new ApiResponse<string>(200, _localizer["Notification.MarkedAsRead"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Marks all notifications for a company as read.
    /// </summary>
    [HttpPut("company/{companyId}/mark-all-read")]
    [AllowAnonymous]
    public async Task<IActionResult> MarkAllAsRead(Guid companyId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            await _notificationService.MarkAllAsReadAsync(decryptedId);
            return Ok(new ApiResponse<string>(200, _localizer["Notification.AllMarkedAsRead"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}


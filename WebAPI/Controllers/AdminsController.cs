using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/admins")]
[Authorize]
public class AdminsController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IAdminService _adminService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalizationService _localizer;
    private readonly IUploadService _uploadService;

    public AdminsController(IAuthenticationService authService, IAdminService adminService,
        ICurrentUserService currentUserService, ILocalizationService localizer,
        IUploadService uploadService)
    {
        _authService = authService;
        _adminService = adminService;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _uploadService = uploadService;
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto request)
    {
        if (request == null) return BadRequest();

        var result = await _authService.RegisterAdminAsync(request);
        return CreatedAtAction(nameof(CreateAdmin), new { id = result.Id }, result);
    }
 
    // ===== Profile Management Endpoints =====

    /// <summary>
    /// Get current user profile with full information
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var profile = await _adminService.GetProfileAsync(_currentUserService.UserId);
        return profile is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(profile);
    }

    /// <summary>
    /// Get current user profile statistics
    /// </summary>
    [HttpGet("me/statistics")]
    public async Task<IActionResult> GetMyStatistics()
    {
        var stats = await _adminService.GetProfileStatisticsAsync(_currentUserService.UserId);
        return Ok(stats);
    }

    /// <summary>
    /// Update current user basic profile information
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest updateData)
    {
        var updated = await _adminService.UpdateProfileAsync(_currentUserService.UserId, updateData);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    /// <summary>
    /// Update current user preferences (language, theme, timezone, etc.)
    /// </summary>
    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpdateMyPreferences([FromBody] UpdatePreferencesRequest request)
    {
        var updated = await _adminService.UpdatePreferencesAsync(_currentUserService.UserId, request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    /// <summary>
    /// Update current user notification preferences
    /// </summary>
    [HttpPut("me/notifications")]
    public async Task<IActionResult> UpdateMyNotificationPreferences([FromBody] UpdateNotificationPreferencesRequest request)
    {
        var updated = await _adminService.UpdateNotificationPreferencesAsync(_currentUserService.UserId, request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    /// <summary>
    /// Upload profile picture for current user
    /// </summary>
    [HttpPost("me/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture([FromBody] UploadProfilePictureRequest request)
    {
        var updated = await _adminService.UploadProfilePictureAsync(_currentUserService.UserId, request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    /// <summary>
    /// Delete profile picture for current user
    /// </summary>
    [HttpDelete("me/profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var updated = await _adminService.DeleteProfilePictureAsync(_currentUserService.UserId);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    /// <summary>
    /// Change current user password
    /// </summary>
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _adminService.ChangePasswordAsync(_currentUserService.UserId, request.CurrentPassword, request.NewPassword);
        return NoContent();
    }

    /// <summary>
    /// Enable 2FA for current user
    /// </summary>
    [HttpPost("me/2fa/enable")]
    public async Task<IActionResult> Enable2FA()
    {
        var result = await _adminService.Generate2FASecretAsync(_currentUserService.UserId);
        return Ok(result);
    }

    /// <summary>
    /// Verify and activate 2FA for current user
    /// </summary>
    [HttpPost("me/2fa/verify")]
    public async Task<IActionResult> Verify2FA([FromBody] Enable2FARequest request)
    {
        var result = await _adminService.Verify2FAAsync(_currentUserService.UserId, request.VerificationCode);
        return result ? Ok(new { message = _localizer["2FAEnabled"] }) : BadRequest(new { message = _localizer["Invalid2FACode"] });
    }

    /// <summary>
    /// Disable 2FA for current user
    /// </summary>
    [HttpPost("me/2fa/disable")]
    public async Task<IActionResult> Disable2FA()
    {
        await _adminService.Disable2FAAsync(_currentUserService.UserId);
        return Ok(new { message = _localizer["2FADisabled"] });
    }

    /// <summary>
    /// Delete current user account (soft delete)
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var request = new GetAdminByIdRequest { AdminId = _currentUserService.UserId };
        await _adminService.DeleteAsync(request);
        return NoContent();
    }

    [HttpPut("{id}/password")]
    [Authorize(Policy = "AdminOrSuperAdmin")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.NewPassword)) return BadRequest();

        var changeRequest = new ChangePasswordByIdRequest { AdminId = id, NewPassword = request.NewPassword };
        await _authService.ChangeAdminPasswordAsync(changeRequest);
        return NoContent();
    }

    // ----- SuperAdmin only management -----

    [HttpGet()]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (admins, meta) = await _adminService.GetAllAsync(page, pageSize, search);
        var pagination = new PaginationDto(meta.ItemsCount, meta.PageSize, meta.CurrentPage);
        return Ok(new { data = admins, pagination });
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var request = new GetAdminByIdRequest { AdminId = id };
        var admin = await _adminService.GetByIdAsync(request);
        return admin is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(admin);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminRequest updateData)
    {
        var request = new UpdateAdminByIdRequest { AdminId = id, UpdateData = updateData };
        var updated = await _adminService.UpdateAsync(request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }


    [HttpPut("{id}/activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var request = new GetAdminByIdRequest { AdminId = id };
        await _adminService.ActivateAsync(request);
        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var request = new GetAdminByIdRequest { AdminId = id };
        await _adminService.DeactivateAsync(request);
        return NoContent();
    }

    [HttpPut("activate-selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateSelected([FromBody] AdminIdsRequest request)
    {
        if (request?.AdminIds == null || !request.AdminIds.Any()) return BadRequest();
        int count = await _adminService.ActivateSelectedAsync(request);
        return Ok(new { count });
    }

    [HttpPut("deactivate-selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeactivateSelected([FromBody] AdminIdsRequest request)
    {
        if (request?.AdminIds == null || !request.AdminIds.Any()) return BadRequest();
        int count = await _adminService.DeactivateSelectedAsync(request);
        return Ok(new { count });
    }

    [HttpPut("activate-all")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateAll()
    {
        int count = await _adminService.ActivateAllAsync();
        return Ok(new { count });
    }

    [HttpPut("deactivate-all")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeactivateAll()
    {
        int count = await _adminService.DeactivateAllAsync();
        return Ok(new { count });
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var request = new ChangePasswordByIdRequest { AdminId = id, NewPassword = "P@ssw0rd" };
        await _adminService.ResetPasswordAsync(request);
        return NoContent();
    }

    [HttpDelete("selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteSelected([FromBody] AdminIdsRequest request)
    {
        if (request?.AdminIds == null || !request.AdminIds.Any()) return BadRequest();
        int count = await _adminService.DeleteSelectedAsync(request);
        return Ok(new { count });
    }

    [HttpDelete("all")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteAll()
    {
        int count = await _adminService.DeleteAllExceptAsync(_currentUserService.UserId);
        return Ok(new { count });
    }
}

using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.DTOs.User;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

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
    private readonly IIdEncryptionService _idEncryption;
    private readonly IUploadService _uploadService;

    public AdminsController(IAuthenticationService authService, IAdminService adminService,
        ICurrentUserService currentUserService, ILocalizationService localizer,
        IIdEncryptionService idEncryption, IUploadService uploadService)
    {
        _authService = authService;
        _adminService = adminService;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _idEncryption = idEncryption;
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
 
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var admin = await _adminService.GetByIdAsync(_currentUserService.UserId);
        return admin is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(admin);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateAdminRequest request)
    {
        var updated = await _adminService.UpdateAsync(_currentUserService.UserId, request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _adminService.ChangePasswordAsync(_currentUserService.UserId, request.CurrentPassword, request.NewPassword);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        await _adminService.DeleteAsync(_currentUserService.UserId);
        return NoContent();
    }

    [HttpPost("users")]
    [Authorize(Policy = "AdminOrSuperAdmin")]
    public async Task<IActionResult> CreateUser([FromBody] RegistrationRequest request)
    {
        if (request == null) return BadRequest();

        if (request.Image?.UploadId != null)
        {
            var url = await _uploadService.ConsumeAsync(request.Image.UploadId);
            if (string.IsNullOrEmpty(url)) return BadRequest();
            request.ImageUrl = url;
        }

        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(CreateUser), new { id = result.Id }, result);
    }

    [HttpPut("{id}/password")]
    [Authorize(Policy = "AdminOrSuperAdmin")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.NewPassword)) return BadRequest();

        await _authService.ChangeAdminPasswordAsync(_idEncryption.Decrypt(id), request.NewPassword);
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
        var admin = await _adminService.GetByIdAsync(_idEncryption.Decrypt(id));
        return admin is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(admin);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminRequest request)
    {
        var updated = await _adminService.UpdateAsync(_idEncryption.Decrypt(id), request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }


    [HttpPut("{id}/activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _adminService.ActivateAsync(_idEncryption.Decrypt(id));
        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _adminService.DeactivateAsync(_idEncryption.Decrypt(id));
        return NoContent();
    }

    [HttpPut("activate-selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateSelected([FromBody] UsersIdsRequest request)
    {
        if (request?.UsersIds == null || !request.UsersIds.Any()) return BadRequest();
        var ids = request.UsersIds.Select(_idEncryption.Decrypt);
        int count = await _adminService.ActivateSelectedAsync(ids);
        return Ok(new { count });
    }

    [HttpPut("deactivate-selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeactivateSelected([FromBody] UsersIdsRequest request)
    {
        if (request?.UsersIds == null || !request.UsersIds.Any()) return BadRequest();
        var ids = request.UsersIds.Select(_idEncryption.Decrypt);
        int count = await _adminService.DeactivateSelectedAsync(ids);
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
        await _adminService.ResetPasswordAsync(_idEncryption.Decrypt(id), "P@ssw0rd");
        return NoContent();
    }

    [HttpDelete("selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteSelected([FromBody] UsersIdsRequest request)
    {
        if (request?.UsersIds == null || !request.UsersIds.Any()) return BadRequest();
        var ids = request.UsersIds.Select(_idEncryption.Decrypt)
            .Where(g => g != _currentUserService.UserId);
        int count = await _adminService.DeleteSelectedAsync(ids);
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

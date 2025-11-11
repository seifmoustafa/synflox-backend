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
 
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        // Use decrypted ID directly - bypass request DTO mapping for internal calls
        var admin = await _adminService.GetByIdAsync(_currentUserService.UserId);
        return admin is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(admin);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateAdminRequest updateData)
    {
        // Use decrypted ID directly - bypass request DTO mapping for internal calls
        var updated = await _adminService.UpdateAsync(_currentUserService.UserId, updateData);
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

using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for admin CRUD operations (SuperAdmin only)
/// Handles create, read, update, delete, activate, deactivate operations
/// Separated from AdminProfileController for Single Responsibility Principle
/// </summary>
[ApiController]
[Route("api/admins")]
[Authorize(Policy = "SuperAdminOnly")]
public class AdminManagementController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IAdminService _adminService;
    private readonly ILocalizationService _localizer;

    public AdminManagementController(
        IAuthenticationService authService,
        IAdminService adminService,
        ILocalizationService localizer)
    {
        _authService = authService;
        _adminService = adminService;
        _localizer = localizer;
    }

    // ===== Read Operations =====

    /// <summary>
    /// Get all admins with pagination and search
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (admins, pagination) = await _adminService.GetAllAsync(page, pageSize, search);
        return Ok(new { data = admins, pagination });
    }

    /// <summary>
    /// Get admin by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var request = new GetAdminByIdRequest { AdminId = id };
        var admin = await _adminService.GetByIdAsync(request);
        return admin is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(admin);
    }

    // ===== Create Operation =====

    /// <summary>
    /// Create new admin
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.RegisterAdminAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // ===== Update Operations =====

    /// <summary>
    /// Update admin by ID
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminRequest updateData)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var request = new UpdateAdminByIdRequest { AdminId = id, UpdateData = updateData };
        var updated = await _adminService.UpdateAsync(request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    // ===== Password Operations =====

    /// <summary>
    /// Change admin password by ID (requires current password)
    /// </summary>
    [HttpPut("{id}/password")]
    [Authorize(Policy = "AdminOrSuperAdmin")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _adminService.ChangePasswordAsync(id, request.CurrentPassword, request.NewPassword);
        return NoContent();
    }

    /// <summary>
    /// Reset admin password (generates secure random password)
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        // Generate secure random password
        var newPassword = GenerateSecurePassword();

        var request = new ChangePasswordByIdRequest { AdminId = id, NewPassword = newPassword };
        await _adminService.ResetPasswordAsync(request);

        return Ok(new { message = _localizer["PasswordResetSuccess"], temporaryPassword = newPassword });
    }

    // ===== Activate/Deactivate Operations =====

    /// <summary>
    /// Activate admin by ID
    /// </summary>
    [HttpPut("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var request = new GetAdminByIdRequest { AdminId = id };
        await _adminService.ActivateAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Deactivate admin by ID
    /// </summary>
    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var request = new GetAdminByIdRequest { AdminId = id };
        await _adminService.DeactivateAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Activate selected admins (bulk operation)
    /// </summary>
    [HttpPut("activate-selected")]
    public async Task<IActionResult> ActivateSelected([FromBody] AdminIdsRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        int count = await _adminService.ActivateSelectedAsync(request);
        return Ok(new { count, message = $"Activated {count} admins" });
    }

    /// <summary>
    /// Deactivate selected admins (bulk operation)
    /// </summary>
    [HttpPut("deactivate-selected")]
    public async Task<IActionResult> DeactivateSelected([FromBody] AdminIdsRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        int count = await _adminService.DeactivateSelectedAsync(request);
        return Ok(new { count, message = $"Deactivated {count} admins (superadmin protected)" });
    }

    /// <summary>
    /// Activate all admins
    /// </summary>
    [HttpPut("activate-all")]
    public async Task<IActionResult> ActivateAll()
    {
        int count = await _adminService.ActivateAllAsync();
        return Ok(new { count, message = $"Activated {count} admins" });
    }

    /// <summary>
    /// Deactivate all admins
    /// </summary>
    [HttpPut("deactivate-all")]
    public async Task<IActionResult> DeactivateAll()
    {
        int count = await _adminService.DeactivateAllAsync();
        return Ok(new { count, message = $"Deactivated {count} admins (superadmin protected)" });
    }

    // ===== Delete Operations =====

    /// <summary>
    /// Delete selected admins (bulk operation)
    /// </summary>
    [HttpDelete("selected")]
    public async Task<IActionResult> DeleteSelected([FromBody] AdminIdsRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        int count = await _adminService.DeleteSelectedAsync(request);
        return Ok(new { count, message = $"Deleted {count} admins (superadmin protected)" });
    }

    /// <summary>
    /// Delete all admins except current user (requires confirmation)
    /// </summary>
    [HttpDelete("all")]
    public async Task<IActionResult> DeleteAll([FromBody] DeleteAllAdminsRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Double-check confirmation text
        if (request.ConfirmationText != "DELETE_ALL_ADMINS")
        {
            return BadRequest(new { message = "Confirmation text must be exactly 'DELETE_ALL_ADMINS'" });
        }

        var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        int count = await _adminService.DeleteAllExceptAsync(currentUserId);
        return Ok(new { count, message = $"Deleted {count} admins (current user & superadmin protected)" });
    }

    // ===== Private Helper Methods =====

    /// <summary>
    /// Generate cryptographically secure random password
    /// </summary>
    private string GenerateSecurePassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "@$!%*?&#";
        const string allChars = uppercase + lowercase + digits + special;

        var result = new char[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            var buffer = new byte[16];
            rng.GetBytes(buffer);

            // Ensure at least one of each required character type
            result[0] = uppercase[buffer[0] % uppercase.Length];
            result[1] = lowercase[buffer[1] % lowercase.Length];
            result[2] = digits[buffer[2] % digits.Length];
            result[3] = special[buffer[3] % special.Length];

            // Fill the rest randomly
            for (int i = 4; i < 16; i++)
            {
                result[i] = allChars[buffer[i] % allChars.Length];
            }

            // Shuffle to randomize position
            for (int i = result.Length - 1; i > 0; i--)
            {
                rng.GetBytes(buffer);
                int j = buffer[0] % (i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
        }

        return new string(result);
    }
}

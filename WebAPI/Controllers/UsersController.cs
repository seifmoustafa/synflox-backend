using System;
using Application.Services;
using Application.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs.Authentication;
using Application.DTOs.Responses;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Endpoints for retrieving and updating the currently authenticated user.
/// </summary>
public class UsersController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;
    private readonly IUploadService _uploadService;

    public UsersController(ICurrentUserService currentUserService,
        IAuthenticationService authenticationService,
        IUserService userService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption,
        IUploadService uploadService)
    {
        _currentUserService = currentUserService;
        _authenticationService = authenticationService;
        _userService = userService;
        _localizer = localizer;
        _idEncryption = idEncryption;
        _uploadService = uploadService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // 1. Extract user ID from the token (via your custom middleware)
        var userId = _currentUserService.UserId;

        // 2. Fetch the UserDto from your IUserService
        UserDto? user = await _userService.GetByIdAsync(userId);

        if (user == null)
            return NotFound(new ApiResponse<string>(StatusCodes.Status404NotFound,
                _localizer["UserNotFound"]));

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateProfileRequest request)
    {
        if (request.Image?.UploadId != null)
        {
            var url = await _uploadService.ConsumeAsync(request.Image.UploadId);
            if (url == null) return BadRequest();
            request.ImageUrl = url;
        }

        var updated = await _authenticationService.UpdateUserAsync(_currentUserService.UserId, request);
        return Ok(updated);
    }

    [HttpPut("me/email")]
    public async Task<IActionResult> ChangeMyEmail([FromBody] ChangeEmailRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await _authenticationService.ChangeEmailAsync(_currentUserService.UserId, request.Email);
        return Ok(updated);
    }

    [HttpPut("me/phone")]
    public async Task<IActionResult> ChangeMyPhone([FromBody] ChangePhoneRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await _authenticationService.ChangePhoneAsync(_currentUserService.UserId, request.PhoneNumber);
        return Ok(updated);
    }

    [HttpPut("me/username")]
    public async Task<IActionResult> ChangeMyUsername([FromBody] ChangeUsernameRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await _authenticationService.ChangeUsernameAsync(_currentUserService.UserId, request.Username);
        return Ok(updated);
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _authenticationService.ChangePasswordAsync(_currentUserService.UserId, request.CurrentPassword, request.NewPassword);
        return NoContent();
    }

    [HttpPut("me/activate")]
    public async Task<IActionResult> ActivateMe()
    {
        await _userService.ActivateAsync(_currentUserService.UserId);
        return NoContent();
    }

    [HttpPut("me/deactivate")]
    public async Task<IActionResult> DeactivateMe()
    {
        await _userService.DeactivateAsync(_currentUserService.UserId);
        return NoContent();
    }

    [HttpDelete("me/image")]
    public async Task<IActionResult> DeleteMyImage()
    {
        await _authenticationService.DeleteUserImageAsync(_currentUserService.UserId);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        await _userService.DeleteAsync(_currentUserService.UserId);
        return NoContent();
    }

    // ----- SuperAdmins only -----

    [HttpGet]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (users, meta) = await _userService.GetAllAsync(page, pageSize, search);
        var pagination = new PaginationDto(meta.ItemsCount, meta.PageSize, meta.CurrentPage);
        return Ok(new { data = users, pagination });
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(_idEncryption.Decrypt(id));
        return user is null
            ? NotFound(new ApiResponse<string>(StatusCodes.Status404NotFound, _localizer["UserNotFound"]))
            : Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var created = await _userService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        dto.Id = _idEncryption.Decrypt(id);
        var updated = await _userService.UpdateAsync(dto);
        return updated is null
            ? NotFound(new ApiResponse<string>(StatusCodes.Status404NotFound, _localizer["UserNotFound"]))
            : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.DeleteAsync(_idEncryption.Decrypt(id));
        return NoContent();
    }

    [HttpDelete("selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteSelected([FromBody] UsersIdsRequest request)
    {
        if (request?.UsersIds == null || !request.UsersIds.Any()) return BadRequest();
        var ids = request.UsersIds.Select(_idEncryption.Decrypt);
        int count = await _userService.DeleteSelectedAsync(ids);
        return Ok(new ApiResponse<int>(StatusCodes.Status200OK, string.Empty, count));
    }

    [HttpDelete("all")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteAll()
    {
        int count = await _userService.DeleteAllAsync();
        return Ok(new ApiResponse<int>(StatusCodes.Status200OK, string.Empty, count));
    }

    [HttpPut("{id}/activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _userService.ActivateAsync(_idEncryption.Decrypt(id));
        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _userService.DeactivateAsync(_idEncryption.Decrypt(id));
        return NoContent();
    }

    [HttpPut("activate-selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateSelected([FromBody] UsersIdsRequest request)
    {
        if (request?.UsersIds == null || !request.UsersIds.Any()) return BadRequest();
        var ids = request.UsersIds.Select(_idEncryption.Decrypt);
        int count = await _userService.ActivateSelectedAsync(ids);
        return Ok(new ApiResponse<int>(StatusCodes.Status200OK, string.Empty, count));
    }

    [HttpPut("deactivate-selected")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeactivateSelected([FromBody] UsersIdsRequest request)
    {
        if (request?.UsersIds == null || !request.UsersIds.Any()) return BadRequest();
        var ids = request.UsersIds.Select(_idEncryption.Decrypt);
        int count = await _userService.DeactivateSelectedAsync(ids);
        return Ok(new ApiResponse<int>(StatusCodes.Status200OK, string.Empty, count));
    }

    [HttpPut("activate-all")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateAll()
    {
        int count = await _userService.ActivateAllAsync();
        return Ok(new ApiResponse<int>(StatusCodes.Status200OK, string.Empty, count));
    }

    [HttpPut("deactivate-all")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeactivateAll()
    {
        int count = await _userService.DeactivateAllAsync();
        return Ok(new ApiResponse<int>(StatusCodes.Status200OK, string.Empty, count));
    }

    // ----- Any authenticated user -----

    [HttpPost("{id}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto)
    {
        var guid = _idEncryption.Decrypt(id);
        if (guid != _currentUserService.UserId)
            return Forbid();
        dto.Id = guid;
        await _userService.ChangePasswordAsync(dto);
        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto)
    {
        dto.Id = _idEncryption.Decrypt(id);
        await _userService.ResetPasswordAsync(dto);
        return NoContent();
    }
}

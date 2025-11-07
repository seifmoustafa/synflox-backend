using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/admin/auth")]
[ApiController]
/// <summary>
/// Handles login and token management for administrators.
/// </summary>
public class AdminAuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserService _currentUserService;

    public AdminAuthenticationController(IAuthenticationService authenticationService,
        ICurrentUserService currentUserService)
    {
        _authenticationService = authenticationService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminAuthenticationRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest();

        var response = await _authenticationService.AdminAuthenticationAsync(request.Username, request.Password);

        if (response.Success)
        {
            return Ok(response);
        }

        return Unauthorized(new ApiResponse<string>(StatusCodes.Status401Unauthorized,
            response.ErrorMessage));
    }

    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        Guid accountId = _currentUserService.UserId;

        if (accountId == Guid.Empty) return BadRequest();

        var response = await _authenticationService.RegenerateAccessToken(accountId);

        if (response.Success)
        {
            return Ok(response);
        }

        return Unauthorized(new ApiResponse<string>(StatusCodes.Status401Unauthorized,
            response.ErrorMessage));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _currentUserService.UserId;
        await _authenticationService.Logout(userId);
        return NoContent();
    }
}

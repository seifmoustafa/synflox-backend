using System;
using System.Threading.Tasks;
using Application.DTOs.Settings;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/password-policy")]
[Authorize(Policy = "SuperAdminOnly")]
public class PasswordPolicyController : ControllerBase
{
    private readonly IPasswordPolicyService _passwordPolicyService;
    private readonly ILocalizationService _localizer;

    public PasswordPolicyController(
        IPasswordPolicyService passwordPolicyService,
        ILocalizationService localizer)
    {
        _passwordPolicyService = passwordPolicyService;
        _localizer = localizer;
    }

    /// <summary>
    /// Gets the active password policy.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActivePolicy()
    {
        var policy = await _passwordPolicyService.GetActivePolicyAsync();
        return Ok(new ApiResponse<PasswordPolicyDto>(200, string.Empty, policy));
    }

    /// <summary>
    /// Updates the password policy.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdatePasswordPolicyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _passwordPolicyService.UpdatePolicyAsync(request);
            return Ok(new ApiResponse<PasswordPolicyDto>(200, _localizer["PasswordPolicy.Updated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Validates a password against the active policy.
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous] // Allow validation without authentication for registration flows
    public async Task<IActionResult> ValidatePassword([FromBody] ValidatePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _passwordPolicyService.ValidatePasswordAsync(request.Password, request.AdminId);
            return Ok(new ApiResponse<PasswordValidationResult>(200, string.Empty, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}


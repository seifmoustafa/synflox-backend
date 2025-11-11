using System;
using System.Threading.Tasks;
using Application.DTOs.Company;
using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/licensing")]
public class LicensingController : ControllerBase
{
    private readonly ILicensingService _licensingService;
    private readonly ILocalizationService _localizer;

    public LicensingController(
        ILicensingService licensingService, 
        ILocalizationService localizer)
    {
        _licensingService = licensingService;
        _localizer = localizer;
    }

    [HttpPut("{id}/activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateCompany(Guid id, [FromBody] ActivateCompanyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            // Set the encrypted ID from route parameter
            request.CompanyId = id;
            var result = await _licensingService.ActivateCompanyAsync(request);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.CompanyActivated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPut("{id}/suspend")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> SuspendCompany(Guid id)
    {
        try
        {
            var request = new SuspendCompanyRequest { CompanyId = id };
            var result = await _licensingService.SuspendCompanyAsync(request);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.CompanySuspended"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPut("{id}/resume")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ResumeCompany(Guid id)
    {
        try
        {
            var request = new ResumeCompanyRequest { CompanyId = id };
            var result = await _licensingService.ResumeCompanyAsync(request);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.CompanyResumed"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPut("{id}/extend")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ExtendCompany(Guid id, [FromBody] ExtendCompanyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            // Set the encrypted ID from route parameter
            request.CompanyId = id;
            var result = await _licensingService.ExtendCompanyAsync(request);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.SubscriptionExtended"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpGet("{id}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanyStatus(Guid id)
    {
        try
        {
            var request = new GetCompanyStatusRequest { CompanyId = id };
            var result = await _licensingService.CheckCompanyStatusAsync(request);
            return Ok(new ApiResponse<CompanyStatusResponse>(200, result.StatusMessage, result));
        }
        catch (Exception ex)
        {
            return NotFound(new ApiResponse<string>(404, ex.Message));
        }
    }

    [HttpPost("{id}/license-key/generate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GenerateLicenseKey(Guid id)
    {
        try
        {
            var request = new GenerateLicenseKeyRequest { CompanyId = id };
            var licenseKey = await _licensingService.GenerateLicenseKeyAsync(request);
            return Ok(new ApiResponse<GenerateLicenseKeyResponse>(200, _localizer["Licensing.LicenseKeyGenerated"], 
                new GenerateLicenseKeyResponse { LicenseKey = licenseKey, Message = _localizer["Licensing.LicenseKeyGenerated"] }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("{id}/license-key/regenerate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> RegenerateLicenseKey(Guid id)
    {
        try
        {
            var request = new GenerateLicenseKeyRequest { CompanyId = id };
            var licenseKey = await _licensingService.RegenerateLicenseKeyAsync(request);
            return Ok(new ApiResponse<GenerateLicenseKeyResponse>(200, _localizer["Licensing.LicenseKeyRegenerated"], 
                new GenerateLicenseKeyResponse { LicenseKey = licenseKey, Message = _localizer["Licensing.LicenseKeyRegenerated"] }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("validate-key")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateLicenseKey([FromBody] ValidateLicenseKeyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        if (request == null || string.IsNullOrWhiteSpace(request.LicenseKey))
        {
            return BadRequest(new ApiResponse<string>(400, _localizer["Licensing.LicenseKeyInvalid"]));
        }

        try
        {
            var result = await _licensingService.ValidateLicenseKeyAsync(request.LicenseKey);
            if (result.IsValid)
            {
                return Ok(new ApiResponse<LicenseKeyValidationResponse>(200, result.Message, result));
            }
            return Unauthorized(new ApiResponse<LicenseKeyValidationResponse>(401, result.Message, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}


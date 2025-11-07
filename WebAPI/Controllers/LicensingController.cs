using System;
using System.Threading.Tasks;
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
    private readonly IIdEncryptionService _idEncryption;

    public LicensingController(
        ILicensingService licensingService, 
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _licensingService = licensingService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    [HttpPost("companies")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _licensingService.CreateCompanyAsync(request);
            return CreatedAtAction(nameof(GetCompany), new { id = result.Id }, 
                new ApiResponse<CompanyDto>(201, _localizer["Licensing.CompanyCreated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpGet("companies")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetAllCompanies([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var (companies, meta) = await _licensingService.GetAllCompaniesAsync(page, pageSize, search);
        return Ok(new ApiResponse<object>(200, string.Empty, new { companies, pagination = meta }));
    }

    [HttpGet("companies/{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetCompany(Guid id)
    {
        var decryptedId = _idEncryption.Decrypt(id);
        var company = await _licensingService.GetCompanyByIdAsync(decryptedId);
        if (company == null)
        {
            return NotFound(new ApiResponse<string>(404, _localizer["Licensing.CompanyNotFound"]));
        }
        return Ok(new ApiResponse<CompanyDto>(200, string.Empty, company));
    }

    [HttpPut("companies/{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _licensingService.UpdateCompanyAsync(decryptedId, request);
            if (result == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Licensing.CompanyNotFound"]));
            }
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.CompanyUpdated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpDelete("companies/{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var deleted = await _licensingService.DeleteCompanyAsync(decryptedId);
            if (!deleted)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Licensing.CompanyNotFound"]));
            }
            return Ok(new ApiResponse<string>(200, _localizer["Licensing.CompanyDeleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPut("{id}/activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateCompany(Guid id, [FromBody] ActivateCompanyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _licensingService.ActivateCompanyAsync(decryptedId, request.ExpiryDate);
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
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _licensingService.SuspendCompanyAsync(decryptedId);
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
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _licensingService.ResumeCompanyAsync(decryptedId);
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
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _licensingService.ExtendCompanyAsync(decryptedId, request.NewExpiryDate);
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
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _licensingService.CheckCompanyStatusAsync(decryptedId);
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
            var decryptedId = _idEncryption.Decrypt(id);
            var licenseKey = await _licensingService.GenerateLicenseKeyAsync(decryptedId);
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
            var decryptedId = _idEncryption.Decrypt(id);
            var licenseKey = await _licensingService.RegenerateLicenseKeyAsync(decryptedId);
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


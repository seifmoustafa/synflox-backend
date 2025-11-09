using System;
using System.Linq;
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
    private readonly ISubscriptionHistoryService _historyService;

    public LicensingController(
        ILicensingService licensingService, 
        ILocalizationService localizer,
        IIdEncryptionService idEncryption,
        ISubscriptionHistoryService historyService)
    {
        _licensingService = licensingService;
        _localizer = localizer;
        _idEncryption = idEncryption;
        _historyService = historyService;
    }

    [HttpPut("{companyId}/activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateCompany(Guid companyId, [FromBody] ActivateCompanyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var result = await _licensingService.ActivateCompanyAsync(decryptedId, request.ExpiryDate);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.CompanyActivated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPut("{companyId}/suspend")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> SuspendCompany(Guid companyId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var result = await _licensingService.SuspendCompanyAsync(decryptedId);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.CompanySuspended"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPut("{companyId}/resume")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ResumeCompany(Guid companyId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var result = await _licensingService.ResumeCompanyAsync(decryptedId);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.CompanyResumed"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPut("{companyId}/extend")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ExtendCompany(Guid companyId, [FromBody] ExtendCompanyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var result = await _licensingService.ExtendCompanyAsync(decryptedId, request.NewExpiryDate);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Licensing.SubscriptionExtended"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets company status by companyId from route (public endpoint - no auth required).
    /// </summary>
    [HttpGet("{companyId}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanyStatus(Guid companyId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var result = await _licensingService.CheckCompanyStatusAsync(decryptedId);
            return Ok(new ApiResponse<CompanyStatusResponse>(200, result.StatusMessage, result));
        }
        catch (Exception ex)
        {
            return NotFound(new ApiResponse<string>(404, ex.Message));
        }
    }

    /// <summary>
    /// Gets company status using API key's companyId (no companyId in route needed).
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetCompanyStatusFromApiKey()
    {
        try
        {
            // Get companyId from API key (set by ApiKeyAuthenticationMiddleware)
            var companyIdFromContext = HttpContext.Items["CompanyId"] as Guid?;
            if (!companyIdFromContext.HasValue)
            {
                return Unauthorized(new ApiResponse<string>(401, _localizer["Authentication.Required"] ?? "API key required"));
            }

            var result = await _licensingService.CheckCompanyStatusAsync(companyIdFromContext.Value);
            return Ok(new ApiResponse<CompanyStatusResponse>(200, result.StatusMessage, result));
        }
        catch (Exception ex)
        {
            return NotFound(new ApiResponse<string>(404, ex.Message));
        }
    }

    [HttpPost("{companyId}/license-key/generate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GenerateLicenseKey(Guid companyId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var licenseKey = await _licensingService.GenerateLicenseKeyAsync(decryptedId);
            return Ok(new ApiResponse<GenerateLicenseKeyResponse>(200, _localizer["Licensing.LicenseKeyGenerated"], 
                new GenerateLicenseKeyResponse { LicenseKey = licenseKey, Message = _localizer["Licensing.LicenseKeyGenerated"] }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("{companyId}/license-key/regenerate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> RegenerateLicenseKey(Guid companyId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
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

    /// <summary>
    /// Gets company history by companyId from route (admin endpoint).
    /// </summary>
    [HttpGet("{companyId}/history")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetCompanyHistory(Guid companyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var (history, meta) = await _historyService.GetHistoryByCompanyIdAsync(decryptedId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { history, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets company history using API key's companyId (no companyId in route needed).
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetCompanyHistoryFromApiKey([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // Get companyId from API key (set by ApiKeyAuthenticationMiddleware)
            var companyIdFromContext = HttpContext.Items["CompanyId"] as Guid?;
            if (!companyIdFromContext.HasValue)
            {
                return Unauthorized(new ApiResponse<string>(401, _localizer["Authentication.Required"] ?? "API key required"));
            }

            var (history, meta) = await _historyService.GetHistoryByCompanyIdAsync(companyIdFromContext.Value, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { history, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets all subscription history (admin only - can filter by companyId).
    /// </summary>
    [HttpGet("history/all")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetAllHistory(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;
            Guid? decryptedCompanyId = null;
            if (companyId.HasValue)
            {
                decryptedCompanyId = _idEncryption.Decrypt(companyId.Value);
            }

            var (history, meta) = await _historyService.GetHistoryByDateRangeAsync(from, to, decryptedCompanyId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { history, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("bulk-activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkActivate([FromBody] BulkOperationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null || request.CompanyIds == null || request.CompanyIds.Count == 0)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidRequest"]));

        if (request.Action != BulkOperationAction.Activate)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidAction"]));

        if (!request.ExpiryDate.HasValue)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.ExpiryDateRequired"]));

        try
        {
            var decryptedIds = request.CompanyIds.Select(id => _idEncryption.Decrypt(id)).ToList();
            var result = await _licensingService.BulkActivateAsync(decryptedIds, request.ExpiryDate.Value);
            return Ok(new ApiResponse<BulkOperationResponse>(200, _localizer["BulkOperation.Completed"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("bulk-suspend")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkSuspend([FromBody] BulkOperationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null || request.CompanyIds == null || request.CompanyIds.Count == 0)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidRequest"]));

        if (request.Action != BulkOperationAction.Suspend)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidAction"]));

        try
        {
            var decryptedIds = request.CompanyIds.Select(id => _idEncryption.Decrypt(id)).ToList();
            var result = await _licensingService.BulkSuspendAsync(decryptedIds);
            return Ok(new ApiResponse<BulkOperationResponse>(200, _localizer["BulkOperation.Completed"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("bulk-resume")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkResume([FromBody] BulkOperationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null || request.CompanyIds == null || request.CompanyIds.Count == 0)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidRequest"]));

        if (request.Action != BulkOperationAction.Resume)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidAction"]));

        try
        {
            var decryptedIds = request.CompanyIds.Select(id => _idEncryption.Decrypt(id)).ToList();
            var result = await _licensingService.BulkResumeAsync(decryptedIds);
            return Ok(new ApiResponse<BulkOperationResponse>(200, _localizer["BulkOperation.Completed"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("bulk-extend")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkExtend([FromBody] BulkOperationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null || request.CompanyIds == null || request.CompanyIds.Count == 0)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidRequest"]));

        if (request.Action != BulkOperationAction.Extend)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidAction"]));

        if (!request.ExpiryDate.HasValue)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.ExpiryDateRequired"]));

        try
        {
            var decryptedIds = request.CompanyIds.Select(id => _idEncryption.Decrypt(id)).ToList();
            var result = await _licensingService.BulkExtendAsync(decryptedIds, request.ExpiryDate.Value);
            return Ok(new ApiResponse<BulkOperationResponse>(200, _localizer["BulkOperation.Completed"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("{companyId}/trial/start")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> StartTrial(Guid companyId, [FromBody] StartTrialRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var result = await _licensingService.StartTrialAsync(decryptedId, request.TrialDays);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Trial.Started"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("{companyId}/trial/convert")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ConvertTrialToActive(Guid companyId, [FromBody] ConvertTrialRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var result = await _licensingService.ConvertTrialToActiveAsync(decryptedId, request.ExpiryDate);
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Trial.Converted"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}


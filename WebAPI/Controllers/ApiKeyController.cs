using System;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/api-keys")]
[Authorize(Policy = "SuperAdminOnly")]
public class ApiKeyController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public ApiKeyController(
        IApiKeyService apiKeyService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _apiKeyService = apiKeyService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    /// <summary>
    /// Creates a new API key for a company.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _apiKeyService.CreateApiKeyAsync(request);
            return CreatedAtAction(nameof(GetApiKey), new { id = result.Id },
                new ApiResponse<CreateApiKeyResponse>(201, result.Message, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets all API keys, optionally filtered by company.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllApiKeys(
        [FromQuery] Guid? companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            Guid? decryptedCompanyId = null;
            if (companyId.HasValue)
            {
                decryptedCompanyId = _idEncryption.Decrypt(companyId.Value);
            }

            var (apiKeys, meta) = await _apiKeyService.GetAllApiKeysAsync(decryptedCompanyId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { apiKeys, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets API keys for a specific company.
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<IActionResult> GetApiKeysByCompany(
        Guid companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var (apiKeys, meta) = await _apiKeyService.GetApiKeysByCompanyAsync(decryptedId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { apiKeys, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets a specific API key by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetApiKey(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            // Get API key by ID - we'll need to get it from repository directly or add a method
            // For now, get all and filter (not ideal but works)
            var (apiKeys, _) = await _apiKeyService.GetAllApiKeysAsync(null, 1, 1000);
            var apiKeyList = apiKeys.ToList();
            // Since IDs are encrypted in DTOs, we need to find by matching the encrypted ID
            var apiKey = apiKeyList.FirstOrDefault(k => k.Id == id);
            
            if (apiKey == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["ApiKey.NotFound"]));
            }
            
            return Ok(new ApiResponse<ApiKeyDto>(200, string.Empty, apiKey));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Revokes (deactivates) an API key.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeApiKey(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            await _apiKeyService.RevokeApiKeyAsync(decryptedId);
            return Ok(new ApiResponse<string>(200, _localizer["ApiKey.Revoked"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Regenerates an API key (creates new key, invalidates old one).
    /// </summary>
    [HttpPut("{id}/regenerate")]
    public async Task<IActionResult> RegenerateApiKey(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _apiKeyService.RegenerateApiKeyAsync(decryptedId);
            return Ok(new ApiResponse<CreateApiKeyResponse>(200, result.Message, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Updates an API key (name, expiration, IP whitelist, rate limit, active status).
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApiKey(Guid id, [FromBody] UpdateApiKeyRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _apiKeyService.UpdateApiKeyAsync(decryptedId, request);
            return Ok(new ApiResponse<ApiKeyDto>(200, _localizer["ApiKey.Updated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.ClientAdmin;
using Application.DTOs.Responses;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for SYNFLOX admins to manage client admin tokens.
/// Used to generate/revoke tokens that client administrators use to manage their devices.
/// </summary>
[ApiController]
[Route("api/admin/offline-license-tokens")]
[Authorize(Policy = "SuperAdminOnly")]
public class OfflineLicenseAdminController : ControllerBase
{
    private readonly IOfflineLicenseAdminService _adminService;
    private readonly IIdEncryptionService _idEncryption;
    private readonly ILocalizationService _localizer;
    private readonly IMapper _mapper;
    private readonly ILogger<OfflineLicenseAdminController> _logger;

    public OfflineLicenseAdminController(
        IOfflineLicenseAdminService adminService,
        IIdEncryptionService idEncryption,
        ILocalizationService localizer,
        IMapper mapper,
        ILogger<OfflineLicenseAdminController> logger)
    {
        _adminService = adminService;
        _idEncryption = idEncryption;
        _localizer = localizer;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new admin token for a company.
    /// This token allows the client admin to manage their device bindings.
    /// </summary>
    /// <param name="request">Token generation request</param>
    /// <returns>Generated token (only shown once!)</returns>
    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<GenerateAdminTokenResponse>>> GenerateToken(
        [FromBody] GenerateAdminTokenRequest request)
    {
        try
        {
            // Decrypt company ID
            var decryptedCompanyId = _idEncryption.Decrypt(request.CompanyId);
            var internalRequest = new GenerateAdminTokenRequest
            {
                CompanyId = decryptedCompanyId,
                Name = request.Name,
                ExpiryDays = request.ExpiryDays,
                CanBindDevices = request.CanBindDevices,
                CanUnbindDevices = request.CanUnbindDevices,
                CanViewDevices = request.CanViewDevices,
                CanApproveReplacements = request.CanApproveReplacements,
                DailyApiLimit = request.DailyApiLimit,
                Notes = request.Notes
            };

            var result = await _adminService.GenerateAdminTokenAsync(internalRequest);
            
            if (result.Success)
            {
                // Encrypt IDs in response
                if (result.TokenInfo != null)
                {
                    result.TokenInfo.Id = _idEncryption.Encrypt(result.TokenInfo.Id);
                    result.TokenInfo.CompanyId = _idEncryption.Encrypt(result.TokenInfo.CompanyId);
                }
                return Ok(ApiResponse<GenerateAdminTokenResponse>.Success(result, result.Message));
            }
            
            return BadRequest(ApiResponse<GenerateAdminTokenResponse>.Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating admin token");
            return BadRequest(ApiResponse<GenerateAdminTokenResponse>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Get all admin tokens for a company.
    /// </summary>
    /// <param name="companyId">Encrypted company ID</param>
    /// <param name="includeRevoked">Include revoked tokens</param>
    /// <returns>List of tokens</returns>
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<ApiResponse<List<AdminTokenDto>>>> GetTokensByCompany(
        Guid companyId,
        [FromQuery] bool includeRevoked = false)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var tokens = await _adminService.GetTokensByCompanyAsync(decryptedId, includeRevoked);
            
            // Encrypt IDs in response
            foreach (var token in tokens)
            {
                token.Id = _idEncryption.Encrypt(token.Id);
                token.CompanyId = _idEncryption.Encrypt(token.CompanyId);
            }
            
            return Ok(ApiResponse<List<AdminTokenDto>>.Success(tokens));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tokens for company {CompanyId}", companyId);
            return BadRequest(ApiResponse<List<AdminTokenDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Get a specific token by ID.
    /// </summary>
    /// <param name="tokenId">Encrypted token ID</param>
    /// <returns>Token details</returns>
    [HttpGet("{tokenId}")]
    public async Task<ActionResult<ApiResponse<AdminTokenDto>>> GetToken(Guid tokenId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(tokenId);
            var token = await _adminService.GetTokenByIdAsync(decryptedId);
            
            if (token == null)
                return NotFound(ApiResponse<AdminTokenDto>.Error(_localizer["OfflineLicense.TokenNotFound"]));
            
            // Encrypt IDs in response
            token.Id = _idEncryption.Encrypt(token.Id);
            token.CompanyId = _idEncryption.Encrypt(token.CompanyId);
            
            return Ok(ApiResponse<AdminTokenDto>.Success(token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token {TokenId}", tokenId);
            return BadRequest(ApiResponse<AdminTokenDto>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Revoke an admin token.
    /// </summary>
    /// <param name="tokenId">Encrypted token ID</param>
    /// <param name="reason">Revocation reason</param>
    /// <returns>Success status</returns>
    [HttpDelete("{tokenId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeToken(
        Guid tokenId,
        [FromQuery] string? reason = null)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(tokenId);
            var result = await _adminService.RevokeAdminTokenAsync(decryptedId, reason ?? "Revoked by admin");
            
            if (result)
            {
                return Ok(ApiResponse<bool>.Success(true, _localizer["OfflineLicense.TokenRevoked"]));
            }
            
            return NotFound(ApiResponse<bool>.Error(_localizer["OfflineLicense.TokenNotFound"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token {TokenId}", tokenId);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
    }
}

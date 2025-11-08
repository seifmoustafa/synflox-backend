using System;
using System.Threading.Tasks;
using Application.DTOs.Analytics;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Policy = "SuperAdminOnly")]
public class AnalyticsController : ControllerBase
{
    private readonly ICompanyUsageAnalyticsService _analyticsService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public AnalyticsController(
        ICompanyUsageAnalyticsService analyticsService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _analyticsService = analyticsService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    /// <summary>
    /// Gets usage analytics for a specific company.
    /// </summary>
    [HttpGet("company/{companyId}/usage")]
    public async Task<IActionResult> GetCompanyUsage(
        Guid companyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var analytics = await _analyticsService.GetCompanyUsageAsync(decryptedId, fromDate, toDate);
            return Ok(new ApiResponse<CompanyUsageAnalyticsDto>(200, string.Empty, analytics));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets overall API usage analytics.
    /// </summary>
    [HttpGet("api-usage")]
    [HttpGet("usage-summary")] // Keep for backward compatibility
    public async Task<IActionResult> GetUsageSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var analytics = await _analyticsService.GetOverallUsageAsync(fromDate, toDate);
            return Ok(new ApiResponse<CompanyUsageAnalyticsDto>(200, string.Empty, analytics));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets usage analytics by endpoint.
    /// </summary>
    [HttpGet("api-usage/by-endpoint")]
    [HttpGet("usage/by-endpoint")] // Keep for backward compatibility
    public async Task<IActionResult> GetUsageByEndpoint(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var usage = await _analyticsService.GetUsageByEndpointAsync(fromDate, toDate);
            return Ok(new ApiResponse<object>(200, string.Empty, new { usage }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets usage analytics by company.
    /// </summary>
    [HttpGet("api-usage/by-company")]
    [HttpGet("usage/by-company")] // Keep for backward compatibility
    public async Task<IActionResult> GetUsageByCompany(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var (analytics, meta) = await _analyticsService.GetUsageByCompanyAsync(fromDate, toDate, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { analytics, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}


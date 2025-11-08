using System;
using System.Threading.Tasks;
using Application.DTOs.Metrics;
using Application.DTOs.Responses;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for system metrics and telemetry.
/// </summary>
[ApiController]
[Route("api/metrics")]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly ILocalizationService _localizer;

    public MetricsController(
        IMetricsService metricsService,
        ILocalizationService localizer)
    {
        _metricsService = metricsService;
        _localizer = localizer;
    }

    /// <summary>
    /// Gets real-time metric summary.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var result = await _metricsService.GetSummaryAsync();
            return Ok(new ApiResponse<MetricSummaryDto>(200, _localizer["Metrics.SummaryRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets historical metrics for a specific metric type.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] MetricType metricType,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? aggregationPeriod = null)
    {
        try
        {
            var result = await _metricsService.GetHistoryAsync(metricType, fromDate, toDate, aggregationPeriod);
            return Ok(new ApiResponse<MetricHistoryDto>(200, _localizer["Metrics.HistoryRetrieved"], result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Triggers metric aggregation (hourly, daily, monthly).
    /// </summary>
    [HttpPost("aggregate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> AggregateMetrics()
    {
        try
        {
            await _metricsService.AggregateMetricsAsync();
            return Ok(new ApiResponse<string>(200, _localizer["Metrics.Aggregated"]));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Cleans up old metrics (keeps only last N days).
    /// </summary>
    [HttpPost("cleanup")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CleanupMetrics([FromQuery] int keepDays = 30)
    {
        try
        {
            await _metricsService.CleanupOldMetricsAsync(keepDays);
            return Ok(new ApiResponse<string>(200, _localizer["Metrics.CleanedUp"]));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }
}




using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.PlanEntitlements;
using Application.DTOs.Responses;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for managing plan-level entitlements
/// These define access rights for all subscriptions of a plan
/// </summary>
[ApiController]
[Route("api/plan-entitlements")]
[Authorize(Policy = "SuperAdminOnly")]
[Produces("application/json")]
public class PlanEntitlementsController : ControllerBase
{
    private readonly IPlanEntitlementService _service;
    private readonly IMapper _mapper;

    public PlanEntitlementsController(IPlanEntitlementService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all entitlements for a specific plan
    /// </summary>
    /// <param name="planId">Plan ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of plan entitlements</returns>
    [HttpGet("plan/{planId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PlanEntitlementDto>>>> GetByPlanId(
        Guid planId, 
        CancellationToken cancellationToken)
    {
        // Decrypt planId using AutoMapper
        var decryptedPlanId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = planId });
        
        var entitlements = await _service.GetByPlanIdAsync(decryptedPlanId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<PlanEntitlementDto>>.Success(entitlements, "Plan entitlements retrieved successfully"));
    }

    /// <summary>
    /// Get a single plan entitlement by ID
    /// </summary>
    /// <param name="id">Entitlement ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plan entitlement details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PlanEntitlementDto>>> GetById(
        Guid id, 
        CancellationToken cancellationToken)
    {
        // Decrypt ID using AutoMapper
        var decryptedId = _mapper.Map<Guid>(new PlanEntitlementIdRequest { EntitlementId = id });
        
        var entitlement = await _service.GetByIdAsync(decryptedId, cancellationToken);
        if (entitlement == null)
            return NotFound(ApiResponse<PlanEntitlementDto>.Error("Plan entitlement not found"));
        
        return Ok(ApiResponse<PlanEntitlementDto>.Success(entitlement, "Plan entitlement retrieved successfully"));
    }

    /// <summary>
    /// Create a new plan entitlement
    /// </summary>
    /// <param name="request">Create request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created plan entitlement</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PlanEntitlementDto>>> Create(
        [FromBody] CreatePlanEntitlementRequest request, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<PlanEntitlementDto>.Error("Invalid request"));

        // Decrypt IDs in request using AutoMapper
        var decryptedRequest = new CreatePlanEntitlementRequest
        {
            PlanId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = request.PlanId }),
            ProjectId = request.ProjectId.HasValue 
                ? _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = request.ProjectId.Value }) 
                : null,
            ModuleId = request.ModuleId.HasValue 
                ? _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = request.ModuleId.Value }) 
                : null,
            AccessLevel = request.AccessLevel,
            CanCreate = request.CanCreate,
            CanRead = request.CanRead,
            CanUpdate = request.CanUpdate,
            CanDelete = request.CanDelete,
            CanExport = request.CanExport,
            DisplayInMenu = request.DisplayInMenu,
            Features = request.Features
        };

        var result = await _service.CreateAsync(decryptedRequest, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, 
            ApiResponse<PlanEntitlementDto>.Success(result, "Plan entitlement created successfully"));
    }

    /// <summary>
    /// Update an existing plan entitlement
    /// </summary>
    /// <param name="id">Entitlement ID (encrypted)</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated plan entitlement</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<PlanEntitlementDto>>> Update(
        Guid id,
        [FromBody] UpdatePlanEntitlementRequest request, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<PlanEntitlementDto>.Error("Invalid request"));

        // Decrypt ID and set in request using AutoMapper
        var decryptedId = _mapper.Map<Guid>(new PlanEntitlementIdRequest { EntitlementId = id });
        request.Id = decryptedId;

        var result = await _service.UpdateAsync(request, cancellationToken);
        return Ok(ApiResponse<PlanEntitlementDto>.Success(result, "Plan entitlement updated successfully"));
    }

    /// <summary>
    /// Delete a plan entitlement
    /// </summary>
    /// <param name="id">Entitlement ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id, 
        CancellationToken cancellationToken)
    {
        // Decrypt ID using AutoMapper
        var decryptedId = _mapper.Map<Guid>(new PlanEntitlementIdRequest { EntitlementId = id });
        
        await _service.DeleteAsync(decryptedId, cancellationToken);
        return Ok(ApiResponse<object>.Success(null, "Plan entitlement deleted successfully"));
    }

    /// <summary>
    /// Delete all entitlements for a plan
    /// </summary>
    /// <param name="planId">Plan ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpDelete("plan/{planId}/all")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAllByPlanId(
        Guid planId, 
        CancellationToken cancellationToken)
    {
        // Decrypt planId using AutoMapper
        var decryptedPlanId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = planId });
        
        await _service.DeleteAllByPlanIdAsync(decryptedPlanId, cancellationToken);
        return Ok(ApiResponse<object>.Success(null, "All plan entitlements deleted successfully"));
    }

    /// <summary>
    /// Copy entitlements from one plan to another
    /// </summary>
    /// <param name="request">Copy request with source and target plan IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("copy")]
    public async Task<ActionResult<ApiResponse<object>>> CopyEntitlements(
        [FromBody] CopyEntitlementsRequest request, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Error("Invalid request"));

        // Decrypt IDs using AutoMapper
        var sourcePlanId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = request.SourcePlanId });
        var targetPlanId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = request.TargetPlanId });
        
        await _service.CopyEntitlementsAsync(sourcePlanId, targetPlanId, cancellationToken);
        return Ok(ApiResponse<object>.Success(null, "Entitlements copied successfully"));
    }

    /// <summary>
    /// Grant full project access to a plan
    /// </summary>
    /// <param name="planId">Plan ID (encrypted)</param>
    /// <param name="projectId">Project ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created entitlement</returns>
    [HttpPost("plan/{planId}/project/{projectId}")]
    public async Task<ActionResult<ApiResponse<PlanEntitlementDto>>> GrantProjectAccess(
        Guid planId, 
        Guid projectId, 
        CancellationToken cancellationToken)
    {
        // Decrypt IDs using AutoMapper
        var decryptedPlanId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = planId });
        var decryptedProjectId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = projectId });
        
        var result = await _service.GrantProjectAccessAsync(decryptedPlanId, decryptedProjectId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, 
            ApiResponse<PlanEntitlementDto>.Success(result, "Project access granted successfully"));
    }

    /// <summary>
    /// Grant full module access to a plan
    /// </summary>
    /// <param name="planId">Plan ID (encrypted)</param>
    /// <param name="moduleId">Module ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created entitlement</returns>
    [HttpPost("plan/{planId}/module/{moduleId}")]
    public async Task<ActionResult<ApiResponse<PlanEntitlementDto>>> GrantModuleAccess(
        Guid planId, 
        Guid moduleId, 
        CancellationToken cancellationToken)
    {
        // Decrypt IDs using AutoMapper
        var decryptedPlanId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = planId });
        var decryptedModuleId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = moduleId });
        
        var result = await _service.GrantModuleAccessAsync(decryptedPlanId, decryptedModuleId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, 
            ApiResponse<PlanEntitlementDto>.Success(result, "Module access granted successfully"));
    }

    /// <summary>
    /// Get entitlement count for a plan
    /// </summary>
    /// <param name="planId">Plan ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of entitlements</returns>
    [HttpGet("plan/{planId}/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetCountByPlanId(
        Guid planId, 
        CancellationToken cancellationToken)
    {
        // Decrypt planId using AutoMapper
        var decryptedPlanId = _mapper.Map<Guid>(new PlanIdForEntitlementRequest { PlanId = planId });
        
        var count = await _service.GetCountByPlanIdAsync(decryptedPlanId, cancellationToken);
        return Ok(ApiResponse<int>.Success(count, "Entitlement count retrieved successfully"));
    }
}

// CopyEntitlementsRequest moved to Application.DTOs.PlanEntitlements.PlanEntitlementIdRequests.cs

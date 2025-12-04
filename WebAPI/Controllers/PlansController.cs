using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.PlanDto;
using Application.DTOs.Subscriptions;
using Application.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/plans")]
[Authorize(Policy = "SuperAdminOnly")]
public class PlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;
    private readonly ILocalizationService _localizer;

    public PlansController(ISubscriptionPlanService planService, ILocalizationService localizer)
    {
        _planService = planService;
        _localizer = localizer;
    }

    [HttpGet]
    [AllowAnonymous] // Allow public access to view plans
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var (plans, meta) = await _planService.GetAllAsync(page, pageSize, search);
        return Ok(new { data = plans, pagination = meta });
    }
    
    /// <summary>
    /// Get all free tier plans (for fallback plan dropdown)
    /// </summary>
    [HttpGet("free-tier")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFreeTierPlans()
    {
        var plans = await _planService.GetFreeTierPlansAsync();
        return Ok(new { data = plans });
    }

    [HttpGet("{id}")]
    [AllowAnonymous] // Allow public access to view plan details
    public async Task<IActionResult> GetById(Guid id)
    {
        // Create request DTO with encrypted ID from route (SYNFLOX ID encryption rule compliance)
        var request = new PlanIdRequest { PlanId = id };
        // Use GetDetailsAsync to include Projects and Modules for edit form
        var plan = await _planService.GetDetailsAsync(request);
        if (plan == null)
            return NotFound(new { message = _localizer["Plan.NotFound"] });

        return Ok(new { data = plan });
    }

    [HttpGet("{id}/details")]
    [AllowAnonymous] // Allow public access to view plan details with projects/modules
    public async Task<IActionResult> GetDetails(Guid id)
    {
        // Create request DTO with encrypted ID from route (SYNFLOX ID encryption rule compliance)
        var request = new PlanIdRequest { PlanId = id };
        var plan = await _planService.GetDetailsAsync(request);
        if (plan == null)
            return NotFound(new { message = _localizer["Plan.NotFound"] });

        return Ok(plan);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var plan = await _planService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = plan.Id }, new { data = plan, message = _localizer["Plan.Created"] });
        }
        catch (PlanModuleConflictException ex)
        {
            // Return 409 Conflict with validation details - frontend should show confirmation dialog
            return Conflict(new { 
                requiresConfirmation = true,
                validationResult = ex.ValidationResult,
                message = _localizer["Plan.ModuleConflictRequiresConfirmation"]
            });
        }
    }
    
    /// <summary>
    /// Create plan with confirmation to remove duplicate modules
    /// </summary>
    [HttpPost("with-confirmation")]
    public async Task<IActionResult> CreateWithConfirmation([FromBody] CreatePlanWithConfirmationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var plan = await _planService.CreateWithConfirmationAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = plan.Id }, new { data = plan, message = _localizer["Plan.Created"] });
        }
        catch (PlanModuleConflictException ex)
        {
            // Return 409 Conflict if user hasn't confirmed yet
            return Conflict(new { 
                requiresConfirmation = true,
                validationResult = ex.ValidationResult,
                message = _localizer["Plan.ModuleConflictRequiresConfirmation"]
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Create request DTO with encrypted ID from route (SYNFLOX ID encryption rule compliance)
            var request = new PlanIdRequest { PlanId = id };
            var plan = await _planService.UpdateAsync(request, dto);
            if (plan == null)
                return NotFound(new { message = _localizer["Plan.NotFound"] });

            return Ok(new { data = plan, message = _localizer["Plan.Updated"] });
        }
        catch (PlanModuleConflictException ex)
        {
            // Return 409 Conflict with validation details - frontend should show confirmation dialog
            return Conflict(new { 
                requiresConfirmation = true,
                validationResult = ex.ValidationResult,
                message = _localizer["Plan.ModuleConflictRequiresConfirmation"]
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Create request DTO with encrypted ID from route (SYNFLOX ID encryption rule compliance)
        var request = new PlanIdRequest { PlanId = id };
        await _planService.DeleteAsync(request);
        return NoContent();
    }
    
    /// <summary>
    /// Validate plan modules for conflicts before save
    /// Returns warnings if any standalone modules are already in projects
    /// </summary>
    [HttpPost("validate-modules")]
    public async Task<IActionResult> ValidateModules([FromBody] ValidatePlanModulesRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var result = await _planService.ValidatePlanModulesAsync(request);
        
        // Return validation result with appropriate status
        if (result.RequiresConfirmation)
        {
            // 200 OK with warning data - frontend decides whether to confirm
            return Ok(new { 
                data = result,
                requiresConfirmation = true,
                message = result.WarningMessage 
            });
        }
        
        return Ok(new { data = result, requiresConfirmation = false });
    }
    
    /// <summary>
    /// Update plan with confirmation to remove duplicate modules
    /// </summary>
    [HttpPut("{id}/with-confirmation")]
    public async Task<IActionResult> UpdateWithConfirmation(Guid id, [FromBody] UpdatePlanWithConfirmationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        try
        {
            var request = new PlanIdRequest { PlanId = id };
            var plan = await _planService.UpdateWithConfirmationAsync(request, dto);
            if (plan == null)
                return NotFound(new { message = _localizer["Plan.NotFound"] });
            
            return Ok(new { data = plan, message = _localizer["Plan.Updated"] });
        }
        catch (PlanModuleConflictException ex)
        {
            // Return 409 Conflict with validation result for frontend to show confirmation dialog
            return Conflict(new { 
                requiresConfirmation = true,
                validationResult = ex.ValidationResult,
                message = _localizer["Plan.ModuleConflictRequiresConfirmation"]
            });
        }
    }
}

using System;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/subscription-plans")]
[Authorize(Policy = "SuperAdminOnly")]
public class SubscriptionPlanController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public SubscriptionPlanController(
        ISubscriptionPlanService planService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _planService = planService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    /// <summary>
    /// Creates a new subscription plan.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _planService.CreatePlanAsync(request);
            return CreatedAtAction(nameof(GetPlan), new { id = result.Id },
                new ApiResponse<SubscriptionPlanDto>(201, _localizer["SubscriptionPlan.Created"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets all subscription plans.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPlans(
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var (plans, meta) = await _planService.GetAllPlansAsync(isActive, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { plans, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets a subscription plan by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var plan = await _planService.GetPlanByIdAsync(decryptedId);
            if (plan == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["SubscriptionPlan.NotFound"]));
            }
            return Ok(new ApiResponse<SubscriptionPlanDto>(200, string.Empty, plan));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Updates a subscription plan.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _planService.UpdatePlanAsync(decryptedId, request);
            if (result == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["SubscriptionPlan.NotFound"]));
            }
            return Ok(new ApiResponse<SubscriptionPlanDto>(200, _localizer["SubscriptionPlan.Updated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Deletes a subscription plan.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var deleted = await _planService.DeletePlanAsync(decryptedId);
            if (!deleted)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["SubscriptionPlan.NotFound"]));
            }
            return Ok(new ApiResponse<string>(200, _localizer["SubscriptionPlan.Deleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPut("{id}/project-modules")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> UpdatePlanProjectModules(Guid id, [FromBody] UpdatePlanProjectModulesDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            // Decrypt ProjectModuleIds (encrypted GUIDs from frontend)
            var decryptedProjectModuleIds = request.ProjectModuleIds.Select(moduleId => _idEncryption.Decrypt(moduleId)).ToList();
            var decryptedRequest = new UpdatePlanProjectModulesDto
            {
                ProjectModuleIds = decryptedProjectModuleIds
            };
            var result = await _planService.UpdatePlanProjectModulesAsync(decryptedId, decryptedRequest);
            return Ok(new ApiResponse<SubscriptionPlanDto>(200, _localizer["SubscriptionPlan.ProjectModulesUpdated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpGet("{id}/project-modules")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetPlanProjectModules(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var (planProjectModules, meta) = await _planService.GetPlanProjectModulesAsync(decryptedId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { planProjectModules, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}


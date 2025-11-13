using System;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Application.Services;
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

    [HttpGet("{id}")]
    [AllowAnonymous] // Allow public access to view plan details
    public async Task<IActionResult> GetById(Guid id)
    {
        // Create request DTO with encrypted ID from route (SYNFLOX ID encryption rule compliance)
        var request = new PlanIdRequest { PlanId = id };
        var plan = await _planService.GetByIdAsync(request);
        if (plan == null)
            return NotFound(new { message = _localizer["Plan.NotFound"] });

        return Ok(plan);
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

        var plan = await _planService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubscriptionPlanDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Create request DTO with encrypted ID from route (SYNFLOX ID encryption rule compliance)
        var request = new PlanIdRequest { PlanId = id };
        var plan = await _planService.UpdateAsync(request, dto);
        if (plan == null)
            return NotFound(new { message = _localizer["Plan.NotFound"] });

        return Ok(plan);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Create request DTO with encrypted ID from route (SYNFLOX ID encryption rule compliance)
        var request = new PlanIdRequest { PlanId = id };
        await _planService.DeleteAsync(request);
        return NoContent();
    }
}

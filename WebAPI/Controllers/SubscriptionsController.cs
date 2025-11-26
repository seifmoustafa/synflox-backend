using System;
using System.Threading.Tasks;
using Application.DTOs.ClientAccess;
using Application.DTOs.Subscriptions;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize(Policy = "SuperAdminOnly")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;

    public SubscriptionsController(ISubscriptionService subscriptionService, IMapper mapper, ILocalizationService localizer)
    {
        _subscriptionService = subscriptionService;
        _mapper = mapper;
        _localizer = localizer;
    }

    /// <summary>
    /// Get all subscriptions with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        // For now, get all subscriptions - in production you might want to filter by company or add more filters
        var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync(page, pageSize, search);
        return Ok(new { data = subscriptions.Items, pagination = subscriptions.Pagination });
    }

    /// <summary>
    /// Create a new subscription for a company
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var subscription = await _subscriptionService.CreateSubscriptionAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
    }

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);
        
        var subscription = await _subscriptionService.GetSubscriptionByIdAsync(decryptedSubscriptionId);
        if (subscription == null)
            return NotFound(new { message = _localizer["Subscription.NotFound"] });

        return Ok(subscription);
    }

    /// <summary>
    /// Get active subscription for a company
    /// </summary>
    [HttpGet("company/{companyId}/active")]
    public async Task<IActionResult> GetActiveByCompany(Guid companyId)
    {
        // Decrypt company ID from route parameter (SYNFLOX ID encryption rule compliance)
        var companyIdRequest = new CompanyIdRequest { CompanyId = companyId };
        var decryptedCompanyId = _mapper.Map<Guid>(companyIdRequest);
        
        var subscription = await _subscriptionService.GetActiveSubscriptionAsync(decryptedCompanyId);
        if (subscription == null)
            return NotFound(new { message = _localizer["Subscription.NoActiveSubscription"] });

        return Ok(subscription);
    }

    /// <summary>
    /// Get all subscriptions for a company (active + historical)
    /// </summary>
    [HttpGet("company/{companyId}/all")]
    public async Task<IActionResult> GetByCompany(Guid companyId)
    {
        // Decrypt company ID from route parameter (SYNFLOX ID encryption rule compliance)
        var companyIdRequest = new CompanyIdRequest { CompanyId = companyId };
        var decryptedCompanyId = _mapper.Map<Guid>(companyIdRequest);
        
        var subscriptions = await _subscriptionService.GetCompanySubscriptionsAsync(decryptedCompanyId);
        return Ok(new { data = subscriptions });
    }

    /// <summary>
    /// Get detailed subscription status with computed status messages
    /// </summary>
    [HttpGet("{id}/status")]
    [AllowAnonymous] // Allow companies to check their own status
    public async Task<IActionResult> GetStatus(Guid id)
    {
        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);
        
        var status = await _subscriptionService.GetSubscriptionStatusAsync(decryptedSubscriptionId);
        if (status == null)
            return NotFound(new { message = _localizer["Subscription.NotFound"] });

        return Ok(status);
    }

    /// <summary>
    /// Renew a subscription (create follow-up or extend in place)
    /// </summary>
    [HttpPut("{id}/renew")]
    public async Task<IActionResult> Renew(Guid id, [FromBody] RenewSubscriptionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var subscription = await _subscriptionService.RenewSubscriptionAsync(id, dto);
        return Ok(subscription);
    }

    /// <summary>
    /// Upgrade subscription to a new plan
    /// Supports FullReplace, Prorated, and Deferred modes
    /// Returns detailed upgrade response with commercial summary
    /// </summary>
    [HttpPut("{id}/upgrade")]
    public async Task<IActionResult> Upgrade(Guid id, [FromBody] UpgradeSubscriptionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _subscriptionService.UpgradeSubscriptionAsync(id, dto);
        return Ok(response);
    }

    /// <summary>
    /// Cancel a subscription immediately
    /// </summary>
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] SubscriptionActionDto dto, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        await _subscriptionService.CancelSubscriptionAsync(decryptedSubscriptionId, dto.Reason ?? "Subscription canceled by administrator", lang);
        return Ok(new { message = _localizer["Subscription.Canceled"], timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Suspend a subscription temporarily
    /// </summary>
    [HttpPut("{id}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SubscriptionActionDto dto, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        await _subscriptionService.SuspendSubscriptionAsync(decryptedSubscriptionId, dto.Reason ?? "Subscription suspended by administrator", lang);
        return Ok(new { message = _localizer["Subscription.Suspended"], timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Resume a suspended subscription
    /// </summary>
    [HttpPut("{id}/resume")]
    public async Task<IActionResult> Resume(Guid id, [FromBody] SubscriptionActionDto dto, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        await _subscriptionService.ResumeSubscriptionAsync(decryptedSubscriptionId, dto.Reason ?? "Subscription resumed by administrator", lang);
        return Ok(new { message = _localizer["Subscription.Resumed"], timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Pause a subscription temporarily (different from suspend - preserves trial time)
    /// </summary>
    [HttpPut("{id}/pause")]
    public async Task<IActionResult> Pause(Guid id, [FromBody] SubscriptionActionDto dto, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        await _subscriptionService.PauseSubscriptionAsync(decryptedSubscriptionId, dto.Reason ?? "Subscription paused by administrator", lang);
        return Ok(new { message = _localizer["Subscription.Paused"], timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Unpause a paused subscription
    /// </summary>
    [HttpPut("{id}/unpause")]
    public async Task<IActionResult> Unpause(Guid id, [FromBody] SubscriptionActionDto dto, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        await _subscriptionService.UnpauseSubscriptionAsync(decryptedSubscriptionId, dto.Reason ?? "Subscription unpaused by administrator", lang);
        return Ok(new { message = _localizer["Subscription.Unpaused"], timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Stop trial and convert to paid subscription immediately
    /// </summary>
    [HttpPut("{id}/stop-trial")]
    public async Task<IActionResult> StopTrial(Guid id, [FromBody] SubscriptionActionDto dto, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        await _subscriptionService.StopTrialAsync(decryptedSubscriptionId, dto.Reason ?? "Trial stopped and converted to paid subscription", lang);
        return Ok(new { message = _localizer["Subscription.TrialStopped"], timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Extend subscription expiry date
    /// </summary>
    [HttpPut("{id}/extend")]
    public async Task<IActionResult> Extend(Guid id, [FromBody] ExtendSubscriptionDto dto, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        var subscription = await _subscriptionService.ExtendSubscriptionAsync(decryptedSubscriptionId, dto, lang);
        return Ok(subscription);
    }

    /// <summary>
    /// Reactivate an expired subscription
    /// </summary>
    [HttpPut("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, [FromBody] SubscriptionActionDto dto, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        await _subscriptionService.ReactivateSubscriptionAsync(decryptedSubscriptionId, dto.Reason ?? "Subscription reactivated by administrator", lang);
        return Ok(new { message = _localizer["Subscription.Reactivated"], timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Get subscription history and audit trail
    /// </summary>
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        var history = await _subscriptionService.GetSubscriptionHistoryAsync(decryptedSubscriptionId);
        return Ok(new { data = history });
    }

    /// <summary>
    /// Get subscription analytics and usage statistics
    /// </summary>
    [HttpGet("{id}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid id, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = id };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);

        var analytics = await _subscriptionService.GetSubscriptionAnalyticsAsync(decryptedSubscriptionId, fromDate, toDate);
        return Ok(analytics);
    }
}

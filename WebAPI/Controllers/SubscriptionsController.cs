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
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _subscriptionService.CancelSubscriptionAsync(id);
        return NoContent();
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Entitlements;
using Application.Services;
using AutoMapper;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers;

/// <summary>
/// Admin controller for managing subscription entitlements
/// Provides CRUD operations and bulk management for subscription entitlements
/// </summary>
[ApiController]
[Route("api/entitlements")]
[Authorize(Policy = "SuperAdminOnly")]
[Produces("application/json")]
public class EntitlementsController : ControllerBase
{
    private readonly IEntitlementService _entitlementService;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<EntitlementsController> _logger;

    public EntitlementsController(
        IEntitlementService entitlementService,
        IMapper mapper,
        ILocalizationService localizer,
        ILogger<EntitlementsController> logger)
    {
        _entitlementService = entitlementService;
        _mapper = mapper;
        _localizer = localizer;
        _logger = logger;
    }

    #region CRUD Operations

    /// <summary>
    /// Get all entitlements for a subscription
    /// </summary>
    /// <param name="subscriptionId">The subscription ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entitlements</returns>
    [HttpGet("subscription/{subscriptionId}")]
    public async Task<IActionResult> GetBySubscription(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            // Decrypt ID via AutoMapper
            var request = new GetSubscriptionEntitlementsRequest { SubscriptionId = subscriptionId };
            var decryptedId = _mapper.Map<Guid>(request);
            var entitlements = await _entitlementService.GetSubscriptionEntitlementsAsync(decryptedId, cancellationToken);
            return Ok(new { data = entitlements });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entitlements for subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Get a single entitlement by ID
    /// </summary>
    /// <param name="id">The entitlement ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entitlement details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            // Decrypt ID via AutoMapper
            var request = new GetEntitlementByIdRequest { EntitlementId = id };
            var decryptedId = _mapper.Map<Guid>(request);
            var entitlement = await _entitlementService.GetEntitlementByIdAsync(decryptedId, cancellationToken);
            
            if (entitlement == null)
                return NotFound(new { message = _localizer["Entitlement.NotFound"] });
            
            return Ok(entitlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entitlement {EntitlementId}", id);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Grant a new entitlement to a subscription
    /// </summary>
    /// <param name="request">The entitlement grant request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created entitlement</returns>
    [HttpPost]
    public async Task<IActionResult> Grant([FromBody] GrantEntitlementRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var entitlement = await _entitlementService.GrantEntitlementAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = entitlement.Id }, entitlement);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting entitlement");
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Update an existing entitlement
    /// </summary>
    /// <param name="id">The entitlement ID (encrypted)</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated entitlement</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEntitlementRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            request.EntitlementId = id;
            var entitlement = await _entitlementService.UpdateEntitlementAsync(request, cancellationToken);
            return Ok(entitlement);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entitlement {EntitlementId}", id);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Revoke an entitlement
    /// </summary>
    /// <param name="id">The entitlement ID (encrypted)</param>
    /// <param name="request">The revoke request with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeEntitlementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            request.EntitlementId = id;
            await _entitlementService.RevokeEntitlementAsync(request, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking entitlement {EntitlementId}", id);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Copy plan entitlements to a subscription
    /// </summary>
    /// <param name="request">The copy request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entitlements copied</returns>
    [HttpPost("copy-from-plan")]
    public async Task<IActionResult> CopyFromPlan([FromBody] CopyPlanEntitlementsRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Decrypt ALL IDs via AutoMapper - no direct DTO access
            var (subscriptionId, planId) = _mapper.Map<(Guid, Guid)>(request);
            
            var count = await _entitlementService.CopyPlanEntitlementsToSubscriptionAsync(
                subscriptionId, 
                planId, 
                cancellationToken);
            
            return Ok(new { count, message = $"Copied {count} entitlements from plan" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying plan entitlements");
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Add upgrade entitlements to a subscription
    /// </summary>
    /// <param name="request">The upgrade request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entitlements added</returns>
    [HttpPost("add-upgrade")]
    public async Task<IActionResult> AddUpgradeEntitlements([FromBody] AddUpgradeEntitlementsRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Decrypt ALL IDs via AutoMapper - no direct DTO access
            var (subscriptionId, planId) = _mapper.Map<(Guid, Guid)>(request);
            
            var count = await _entitlementService.AddUpgradeEntitlementsAsync(
                subscriptionId, 
                planId,
                cancellationToken);
            
            return Ok(new { count, message = $"Added {count} upgrade entitlements" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding upgrade entitlements");
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Replace all entitlements for a subscription with new plan's entitlements
    /// </summary>
    /// <param name="request">The replace request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entitlements replaced</returns>
    [HttpPost("replace")]
    public async Task<IActionResult> ReplaceEntitlements([FromBody] ReplaceEntitlementsRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Decrypt ALL IDs via AutoMapper - no direct DTO access
            var (subscriptionId, planId, keepCustomGrants) = _mapper.Map<(Guid, Guid, bool)>(request);
            
            var count = await _entitlementService.ReplaceEntitlementsAsync(
                subscriptionId, 
                planId,
                keepCustomGrants,
                cancellationToken);
            
            return Ok(new { count, message = $"Replaced with {count} new entitlements" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing entitlements");
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Downgrade subscription to fallback plan entitlements
    /// </summary>
    /// <param name="request">The downgrade request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entitlements in fallback</returns>
    [HttpPost("downgrade-to-fallback")]
    public async Task<IActionResult> DowngradeToFallback([FromBody] DowngradeToFallbackRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Decrypt ID via AutoMapper - no direct DTO access
            var subscriptionId = _mapper.Map<Guid>(request);
            
            var count = await _entitlementService.DowngradeToFallbackAsync(
                subscriptionId,
                cancellationToken);
            
            return Ok(new { count, message = $"Downgraded to fallback with {count} entitlements" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading to fallback");
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Bulk revoke multiple entitlements
    /// </summary>
    /// <param name="request">The bulk revoke request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("bulk-revoke")]
    public async Task<IActionResult> BulkRevoke([FromBody] BulkRevokeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var count = await _entitlementService.BulkRevokeAsync(request, cancellationToken);
            return Ok(new { count, message = $"Revoked {count} entitlements" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk revoking entitlements");
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    #endregion

    #region Access Mode & Versioning

    /// <summary>
    /// Get the full entitlement matrix for a subscription (admin view)
    /// </summary>
    /// <param name="subscriptionId">The subscription ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete entitlement matrix</returns>
    [HttpGet("subscription/{subscriptionId}/matrix")]
    public async Task<IActionResult> GetEntitlementMatrix(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            // Decrypt ID via AutoMapper
            var request = new GetEntitlementMatrixRequest { SubscriptionId = subscriptionId };
            var decryptedId = _mapper.Map<Guid>(request);
            var matrix = await _entitlementService.BuildEntitlementMatrixAsync(decryptedId, true, cancellationToken);
            return Ok(matrix);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entitlement matrix for subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Get the current access mode for a subscription
    /// </summary>
    /// <param name="subscriptionId">The subscription ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current access mode and days remaining</returns>
    [HttpGet("subscription/{subscriptionId}/access-mode")]
    public async Task<IActionResult> GetAccessMode(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            // Decrypt ID via AutoMapper
            var request = new GetAccessModeRequest { SubscriptionId = subscriptionId };
            var decryptedId = _mapper.Map<Guid>(request);
            var mode = await _entitlementService.GetEffectiveAccessModeAsync(decryptedId, cancellationToken);
            var daysRemaining = await _entitlementService.GetDaysRemainingInCurrentModeAsync(decryptedId, cancellationToken);
            var isGracePeriod = await _entitlementService.IsInGracePeriodAsync(decryptedId, cancellationToken);
            
            return Ok(new
            {
                accessMode = mode.ToString(),
                daysRemaining,
                isInGracePeriod = isGracePeriod
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access mode for subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Update the access mode for a subscription
    /// </summary>
    /// <param name="subscriptionId">The subscription ID (encrypted)</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("subscription/{subscriptionId}/access-mode")]
    public async Task<IActionResult> UpdateAccessMode(Guid subscriptionId, [FromBody] UpdateAccessModeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Decrypt ID and get values via AutoMapper - no direct DTO access
            request.SubscriptionId = subscriptionId;
            var (decryptedId, accessMode, restrictionMessage) = _mapper.Map<(Guid, Domain.Enums.SubscriptionAccessMode, string?)>(request);
            await _entitlementService.UpdateAccessModeAsync(
                decryptedId,
                accessMode,
                restrictionMessage,
                cancellationToken);
            
            return Ok(new { message = "Access mode updated successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating access mode for subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Get the current entitlements version for a subscription
    /// </summary>
    /// <param name="subscriptionId">The subscription ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current version number</returns>
    [HttpGet("subscription/{subscriptionId}/version")]
    public async Task<IActionResult> GetVersion(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            // Decrypt ID via AutoMapper
            var request = new GetEntitlementsVersionRequest { SubscriptionId = subscriptionId };
            var decryptedId = _mapper.Map<Guid>(request);
            var version = await _entitlementService.GetEntitlementsVersionAsync(decryptedId, cancellationToken);
            return Ok(new { version });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entitlements version for subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Manually increment the entitlements version (forces client cache refresh)
    /// </summary>
    /// <param name="subscriptionId">The subscription ID (encrypted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New version number</returns>
    [HttpPost("subscription/{subscriptionId}/increment-version")]
    public async Task<IActionResult> IncrementVersion(Guid subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            // Decrypt ID via AutoMapper
            var request = new IncrementVersionRequest { SubscriptionId = subscriptionId };
            var decryptedId = _mapper.Map<Guid>(request);
            var newVersion = await _entitlementService.IncrementVersionAsync(decryptedId, cancellationToken);
            return Ok(new { version = newVersion, message = "Version incremented successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing version for subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    #endregion

    #region Access Checking (Admin Tools)

    /// <summary>
    /// Check if a subscription has access to a specific project
    /// </summary>
    /// <param name="request">The access check request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access check result</returns>
    [HttpPost("check-project-access")]
    public async Task<IActionResult> CheckProjectAccess([FromBody] CheckProjectAccessRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Decrypt ALL IDs via AutoMapper - no direct DTO access
            var (subscriptionId, projectId) = _mapper.Map<(Guid, Guid)>(request);
            
            var result = await _entitlementService.HasProjectAccessAsync(subscriptionId, projectId, cancellationToken);
            return Ok(new { hasAccess = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking project access");
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    /// <summary>
    /// Check if a subscription has access to a specific module
    /// </summary>
    /// <param name="request">The access check request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access check result</returns>
    [HttpPost("check-module-access")]
    public async Task<IActionResult> CheckModuleAccess([FromBody] CheckModuleAccessRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Decrypt ALL IDs via AutoMapper - no direct DTO access
            var (subscriptionId, projectId, moduleId) = _mapper.Map<(Guid, Guid?, Guid)>(request);
            
            bool result;
            if (projectId.HasValue)
            {
                result = await _entitlementService.HasModuleAccessAsync(subscriptionId, projectId.Value, moduleId, cancellationToken);
            }
            else
            {
                result = await _entitlementService.HasStandaloneModuleAccessAsync(subscriptionId, moduleId, cancellationToken);
            }
            return Ok(new { hasAccess = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking module access");
            return StatusCode(500, new { message = _localizer["Error.InternalServer"] });
        }
    }

    #endregion
}

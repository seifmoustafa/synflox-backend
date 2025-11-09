using System;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Webhooks;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/webhooks")]
[Authorize(Policy = "SuperAdminOnly")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public WebhookController(
        IWebhookService webhookService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _webhookService = webhookService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            // Note: CompanyId decryption is handled by AutoMapper (DecryptGuidConverter)
            // No need to decrypt here as it would cause double decryption
            var result = await _webhookService.CreateWebhookAsync(request);
            return CreatedAtAction(nameof(GetWebhook), new { id = result.Id },
                new ApiResponse<WebhookDto>(201, _localizer["Webhook.Created"], result));
        }
        catch (Domain.Exceptions.NotFoundException ex)
        {
            return NotFound(new ApiResponse<string>(404, ex.Message));
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets all webhooks, optionally filtered by company.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllWebhooks(
        [FromQuery] Guid? companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            Guid? decryptedCompanyId = null;
            if (companyId.HasValue)
            {
                decryptedCompanyId = _idEncryption.Decrypt(companyId.Value);
            }

            var (webhooks, meta) = await _webhookService.GetAllWebhooksAsync(decryptedCompanyId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { webhooks, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets webhooks for a specific company.
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<IActionResult> GetWebhooksByCompany(
        Guid companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var (webhooks, meta) = await _webhookService.GetWebhooksByCompanyAsync(decryptedId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { webhooks, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets a specific webhook by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWebhook(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var (webhooks, _) = await _webhookService.GetAllWebhooksAsync(null, 1, 1000);
            var webhook = webhooks.FirstOrDefault(w => w.Id == id);
            
            if (webhook == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Webhook.NotFound"]));
            }
            
            return Ok(new ApiResponse<WebhookDto>(200, string.Empty, webhook));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            await _webhookService.DeleteWebhookAsync(decryptedId);
            return Ok(new ApiResponse<string>(200, _localizer["Webhook.Deleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets delivery history for a webhook.
    /// </summary>
    [HttpGet("{id}/deliveries")]
    public async Task<IActionResult> GetDeliveryHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var (deliveries, meta) = await _webhookService.GetDeliveryHistoryAsync(decryptedId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { deliveries, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Retries failed webhook deliveries.
    /// </summary>
    [HttpPost("retry-failed")]
    public async Task<IActionResult> RetryFailedWebhooks([FromQuery] int maxRetries = 3)
    {
        try
        {
            await _webhookService.RetryFailedWebhooksAsync(maxRetries);
            return Ok(new ApiResponse<string>(200, _localizer["Webhook.RetryInitiated"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}


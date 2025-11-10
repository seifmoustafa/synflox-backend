using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Webhooks;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Webhooks;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly IWebhookRepository _repository;
    private readonly IWebhookDeliveryRepository _deliveryRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IWebhookRepository repository,
        IWebhookDeliveryRepository deliveryRepository,
        ICompanyRepository companyRepository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _repository = repository;
        _deliveryRepository = deliveryRepository;
        _companyRepository = companyRepository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<WebhookDto> CreateWebhookAsync(CreateWebhookRequest request)
    {
        // Map DTO to Entity using AutoMapper (automatically decrypts CompanyId)
        var entity = _mapper.Map<Webhook>(request);
        
        // Validate that the company exists
        var company = await _companyRepository.GetByIdAsync(entity.CompanyId, null);
        if (company == null || company.IsDeleted)
        {
            // Log the decrypted company ID for debugging
            _logger.LogWarning("Company not found for ID: {CompanyId} (decrypted from request)", entity.CompanyId);
            throw new NotFoundException(_localizer["Company.NotFound"]);
        }
        
        entity.Id = Guid.NewGuid();
        
        // Use provided secret or generate a new one for HMAC signing
        if (string.IsNullOrWhiteSpace(entity.Secret))
        {
            entity.Secret = GenerateSecret();
        }
        
        entity.IsActive = request.IsActive;
        entity.IsDeleted = false;

        // Ensure Events is never null (should be handled by mapper, but double-check)
        if (string.IsNullOrEmpty(entity.Events))
        {
            entity.Events = "[]";
        }

        var created = await _repository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WebhookDto>(created);
    }

    public async Task<(IEnumerable<WebhookDto> Webhooks, PaginationMetadata Meta)> GetWebhooksByCompanyAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var webhooks = await _repository.GetByCompanyIdAsync(companyId, skip, pageSize);
        var totalCount = await _repository.CountByCompanyIdAsync(companyId);

        var dtos = _mapper.Map<IEnumerable<WebhookDto>>(webhooks);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    public async Task<(IEnumerable<WebhookDto> Webhooks, PaginationMetadata Meta)> GetAllWebhooksAsync(
        Guid? companyId = null,
        int page = 1,
        int pageSize = 10,
        string? search = null)
    {
        if (companyId.HasValue)
        {
            return await GetWebhooksByCompanyAsync(companyId.Value, page, pageSize);
        }

        Expression<Func<Webhook, object?>>[] searchColumns =
        {
            w => w.Url,
            w => w.Company != null ? w.Company.Name : null,
            w => w.Company != null ? w.Company.ContactEmail : null,
        };

        var (entities, meta) = await _repository.GetAllAsync(null, page, pageSize, search, default, searchColumns);
        var dtos = _mapper.Map<IEnumerable<WebhookDto>>(entities);
        return (dtos, meta);
    }

    public async Task DeleteWebhookAsync(Guid webhookId)
    {
        var webhook = await _repository.GetByIdAsync(webhookId, null);
        if (webhook == null || webhook.IsDeleted)
        {
            throw new NotFoundException(_localizer["Webhook.NotFound"]);
        }

        await _repository.DeleteAsync(webhookId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task TriggerWebhookAsync(Guid companyId, WebhookEventType eventType, object payload)
    {
        var webhooks = await _repository.GetActiveWebhooksByCompanyAndEventAsync(companyId, eventType);
        var webhookList = webhooks.ToList();

        if (webhookList.Count == 0)
        {
            return; // No webhooks subscribed to this event
        }

        var payloadJson = JsonSerializer.Serialize(payload);
        var tasks = webhookList.Select(webhook => SendWebhookAsync(webhook, eventType, payloadJson, 1));
        await Task.WhenAll(tasks);
    }

    public async Task RetryFailedWebhooksAsync(int maxRetries = 3)
    {
        var failedDeliveries = await _deliveryRepository.GetFailedDeliveriesForRetryAsync(maxRetries);
        var deliveryList = failedDeliveries.ToList();

        _logger.LogInformation("Retrying {Count} failed webhook deliveries", deliveryList.Count);

        foreach (var delivery in deliveryList)
        {
            try
            {
                var webhook = await _repository.GetByIdAsync(delivery.WebhookId, null);
                if (webhook == null || !webhook.IsActive || webhook.IsDeleted)
                {
                    continue;
                }

                // Calculate exponential backoff delay
                var delayMinutes = CalculateBackoffDelay(delivery.AttemptNumber);
                var lastAttempt = delivery.AttemptedAt;
                if (DateTime.UtcNow < lastAttempt.AddMinutes(delayMinutes))
                {
                    continue; // Not time to retry yet
                }

                // Retry the delivery
                await SendWebhookAsync(webhook, delivery.EventType, delivery.Payload, delivery.AttemptNumber + 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying webhook delivery {DeliveryId}", delivery.Id);
            }
        }
    }

    public async Task<(IEnumerable<WebhookDeliveryDto> Deliveries, PaginationMetadata Meta)> GetDeliveryHistoryAsync(
        Guid webhookId,
        int page = 1,
        int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var deliveries = await _deliveryRepository.GetByWebhookIdAsync(webhookId, skip, pageSize);
        var totalCount = await _deliveryRepository.CountByWebhookIdAsync(webhookId);

        var dtos = _mapper.Map<IEnumerable<WebhookDeliveryDto>>(deliveries);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    private async Task SendWebhookAsync(Webhook webhook, WebhookEventType eventType, string payloadJson, int attemptNumber)
    {
        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookId = webhook.Id,
            EventType = eventType,
            Payload = payloadJson,
            AttemptedAt = DateTime.UtcNow,
            Succeeded = false,
            AttemptNumber = attemptNumber,
            IsDeleted = false
        };

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Generate HMAC signature
            var signature = GenerateHmacSignature(payloadJson, webhook.Secret);
            var timestamp = DateTime.UtcNow.ToString("O");

            // Create request
            var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };

            // Add headers
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Timestamp", timestamp);
            request.Headers.Add("X-Webhook-Event", eventType.ToString());

            // Send request
            var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            delivery.StatusCode = (int)response.StatusCode;
            delivery.ResponseBody = responseBody.Length > 2000 ? responseBody.Substring(0, 2000) : responseBody;
            delivery.Succeeded = response.IsSuccessStatusCode;

            if (!delivery.Succeeded)
            {
                delivery.ErrorMessage = $"HTTP {response.StatusCode}: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}";
            }

            // Update webhook last triggered
            webhook.LastTriggeredAt = DateTime.UtcNow;
            await _repository.UpdateAsync(webhook);
        }
        catch (Exception ex)
        {
            delivery.ErrorMessage = ex.Message.Length > 1000 ? ex.Message.Substring(0, 1000) : ex.Message;
            delivery.Succeeded = false;
            _logger.LogError(ex, "Failed to send webhook {WebhookId} for event {EventType}", webhook.Id, eventType);
        }
        finally
        {
            await _deliveryRepository.AddAsync(delivery);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private static string GenerateHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateSecret()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static int CalculateBackoffDelay(int attemptNumber)
    {
        // Exponential backoff: 1min, 5min, 30min
        return attemptNumber switch
        {
            1 => 1,
            2 => 5,
            _ => 30
        };
    }
}


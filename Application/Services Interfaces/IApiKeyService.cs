using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing API keys.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Creates a new API key for a company.
    /// </summary>
    Task<CreateApiKeyResponse> CreateApiKeyAsync(CreateApiKeyRequest request);

    /// <summary>
    /// Gets all API keys for a company.
    /// </summary>
    Task<(IEnumerable<ApiKeyDto> ApiKeys, PaginationMetadata Meta)> GetApiKeysByCompanyAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets all API keys (for admins).
    /// </summary>
    Task<(IEnumerable<ApiKeyDto> ApiKeys, PaginationMetadata Meta)> GetAllApiKeysAsync(
        Guid? companyId = null,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Revokes (deactivates) an API key.
    /// </summary>
    Task RevokeApiKeyAsync(Guid apiKeyId);

    /// <summary>
    /// Regenerates an API key (creates new key, revokes old one).
    /// </summary>
    Task<CreateApiKeyResponse> RegenerateApiKeyAsync(Guid apiKeyId);

    /// <summary>
    /// Validates an API key and returns the company ID if valid.
    /// </summary>
    Task<(bool IsValid, Guid? CompanyId, ApiKeyDto? ApiKey)> ValidateApiKeyAsync(string apiKey);

    /// <summary>
    /// Updates the last used timestamp for an API key.
    /// </summary>
    Task UpdateLastUsedAsync(Guid apiKeyId);

    /// <summary>
    /// Updates an API key (name, expiration, IP whitelist, rate limit, active status).
    /// </summary>
    Task<ApiKeyDto> UpdateApiKeyAsync(Guid apiKeyId, UpdateApiKeyRequest request);
}


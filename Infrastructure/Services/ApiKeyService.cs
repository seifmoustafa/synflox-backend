using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Application.Services;
using AutoMapper;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDBContext _context;

    public ApiKeyService(
        IApiKeyRepository repository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ApplicationDBContext context)
    {
        _repository = repository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<CreateApiKeyResponse> CreateApiKeyAsync(CreateApiKeyRequest request)
    {
        // Generate API key: "sk_live_" + 32 random bytes (Base64) = ~44 characters
        var apiKey = GenerateApiKey();
        var keyHash = HashApiKey(apiKey);
        var keyPrefix = apiKey.Substring(0, Math.Min(8, apiKey.Length));

        // Generate signing secret: 32 random bytes (Base64) = ~44 characters
        var signingSecret = GenerateSigningSecret();

        // Map DTO to Entity using AutoMapper (automatically decrypts CompanyId)
        var entity = _mapper.Map<ApiKey>(request);
        entity.Id = Guid.NewGuid();
        entity.KeyHash = keyHash;
        entity.KeyPrefix = keyPrefix;
        entity.SigningSecret = signingSecret;
        entity.IsActive = true;
        entity.IsDeleted = false;

        var created = await _repository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<CreateApiKeyResponse>(created);
        
        // Ensure API key is set (defensive check)
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Failed to generate API key");
        }
        
        response.ApiKey = apiKey;
        response.KeyPrefix = keyPrefix;
        response.SigningSecret = signingSecret;
        response.Message = _localizer["ApiKey.Created"];
        
        // Verify the response has the API key before returning
        if (string.IsNullOrEmpty(response.ApiKey))
        {
            throw new InvalidOperationException("API key was not set in response");
        }
        
        return response;
    }

    public async Task<(IEnumerable<ApiKeyDto> ApiKeys, PaginationMetadata Meta)> GetApiKeysByCompanyAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var apiKeys = await _repository.GetByCompanyIdAsync(companyId, skip, pageSize);
        var totalCount = await _repository.CountByCompanyIdAsync(companyId);

        var dtos = _mapper.Map<IEnumerable<ApiKeyDto>>(apiKeys);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    public async Task<(IEnumerable<ApiKeyDto> ApiKeys, PaginationMetadata Meta)> GetAllApiKeysAsync(
        Guid? companyId = null,
        int page = 1,
        int pageSize = 10)
    {
        // For simplicity, if companyId is provided, use GetApiKeysByCompanyAsync
        if (companyId.HasValue)
        {
            return await GetApiKeysByCompanyAsync(companyId.Value, page, pageSize);
        }

        // Otherwise, get all with pagination
        var (entities, meta) = await _repository.GetAllAsync(null, page, pageSize, null, default);
        var dtos = _mapper.Map<IEnumerable<ApiKeyDto>>(entities);
        return (dtos, meta);
    }

    public async Task RevokeApiKeyAsync(Guid apiKeyId)
    {
        var apiKey = await _repository.GetByIdAsync(apiKeyId, null);
        if (apiKey == null || apiKey.IsDeleted)
        {
            throw new NotFoundException(_localizer["ApiKey.NotFound"]);
        }

        apiKey.IsActive = false;
        await _repository.UpdateAsync(apiKey);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CreateApiKeyResponse> RegenerateApiKeyAsync(Guid apiKeyId)
    {
        var apiKey = await _repository.GetByIdAsync(apiKeyId, null);
        if (apiKey == null || apiKey.IsDeleted)
        {
            throw new NotFoundException(_localizer["ApiKey.NotFound"]);
        }

        // Generate new key
        var newApiKey = GenerateApiKey();
        var keyHash = HashApiKey(newApiKey);
        var keyPrefix = newApiKey.Substring(0, Math.Min(8, newApiKey.Length));

        // Generate new signing secret
        var newSigningSecret = GenerateSigningSecret();

        // Update existing key (revoke old one, set new hash)
        apiKey.KeyHash = keyHash;
        apiKey.KeyPrefix = keyPrefix;
        apiKey.SigningSecret = newSigningSecret;
        apiKey.IsActive = true;
        apiKey.LastUsedAt = null; // Reset last used
        await _repository.UpdateAsync(apiKey);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<CreateApiKeyResponse>(apiKey);
        
        // Ensure API key is set (defensive check)
        if (string.IsNullOrEmpty(newApiKey))
        {
            throw new InvalidOperationException("Failed to generate new API key");
        }
        
        response.ApiKey = newApiKey;
        response.KeyPrefix = keyPrefix;
        response.SigningSecret = newSigningSecret;
        response.Message = _localizer["ApiKey.Regenerated"];
        
        // Verify the response has the API key before returning
        if (string.IsNullOrEmpty(response.ApiKey))
        {
            throw new InvalidOperationException("API key was not set in response");
        }
        
        return response;
    }

    public async Task<(bool IsValid, Guid? CompanyId, ApiKeyDto? ApiKey)> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, null, null);
        }

        var keyHash = HashApiKey(apiKey);
        var entity = await _repository.GetByKeyHashAsync(keyHash);

        if (entity == null || entity.IsDeleted || !entity.IsActive)
        {
            return (false, null, null);
        }

        // Check expiration
        if (entity.ExpiresAt.HasValue && entity.ExpiresAt.Value < DateTime.UtcNow)
        {
            return (false, null, null);
        }

        // Update last used timestamp to exact server local time when API key is used
        // This records the precise moment the API key was validated/used using server's local time
        // Set it the same way UpdatedTimestamp is set in other services
        entity.LastUsedAt = DateTime.Now;
        
        // Check if entity is already tracked - if so, just mark as modified
        // If not tracked, use UpdateAsync to attach and mark as modified
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            await _repository.UpdateAsync(entity);
        }
        else
        {
            // Entity is already tracked, just mark the properties as modified
            entry.Property(e => e.LastUsedAt).IsModified = true;
        }
        
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<ApiKeyDto>(entity);
        return (true, entity.CompanyId, dto);
    }

    public async Task UpdateLastUsedAsync(Guid apiKeyId)
    {
        var apiKey = await _repository.GetByIdAsync(apiKeyId, null);
        if (apiKey != null && !apiKey.IsDeleted)
        {
            // Record the exact server local time when the API key is used
            var exactUsageTime = DateTime.Now;
            apiKey.LastUsedAt = exactUsageTime;
            
            // Use repository UpdateAsync to ensure the entity change is properly tracked
            await _repository.UpdateAsync(apiKey);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<ApiKeyDto> UpdateApiKeyAsync(Guid apiKeyId, UpdateApiKeyRequest request)
    {
        var apiKey = await _repository.GetByIdAsync(apiKeyId, null);
        if (apiKey == null || apiKey.IsDeleted)
        {
            throw new NotFoundException(_localizer["ApiKey.NotFound"]);
        }

        _mapper.Map(request, apiKey);
        await _repository.UpdateAsync(apiKey);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ApiKeyDto>(apiKey);
    }

    private static string GenerateApiKey()
    {
        // Generate: "sk_live_" + 32 random bytes (Base64 encoded)
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        var base64 = Convert.ToBase64String(bytes);
        return $"sk_live_{base64}";
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string GenerateSigningSecret()
    {
        // Generate: 32 random bytes (Base64 encoded) = ~44 characters
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}


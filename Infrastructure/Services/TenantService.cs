using System;
using System.Threading.Tasks;
using Application.DTOs.Tenancy;
using Application.Services;
using AutoMapper;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    public TenantService(
        ITenantRepository repository,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
    }

    public async Task<TenantDto?> GetCurrentTenantAsync()
    {
        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue)
            return null;

        var tenant = await _repository.GetByIdAsync(tenantId.Value, null);
        if (tenant == null || tenant.IsDeleted)
            return null;

        return _mapper.Map<TenantDto>(tenant);
    }

    public Task SetTenantContextAsync(Guid tenantId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items["TenantId"] = tenantId;
        }
        return Task.CompletedTask;
    }

    public async Task<TenantDto?> GetTenantByIdAsync(Guid tenantId)
    {
        var tenant = await _repository.GetByIdAsync(tenantId, null);
        if (tenant == null || tenant.IsDeleted)
            return null;

        return _mapper.Map<TenantDto>(tenant);
    }

    private Guid? GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Try to get from HttpContext.Items (set by middleware)
        if (httpContext.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantId)
            return tenantId;

        // Try to get from JWT claim
        var tenantIdClaim = httpContext.User?.FindFirst("TenantId")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var parsedTenantId))
            return parsedTenantId;

        // Try to get from header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue))
        {
            if (Guid.TryParse(headerValue.ToString(), out var headerTenantId))
                return headerTenantId;
        }

        return null;
    }
}



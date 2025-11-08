using System;
using System.Threading.Tasks;
using Application.DTOs.Tenancy;

namespace Application.Services;

/// <summary>
/// Service interface for managing tenants.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets the current tenant from context.
    /// </summary>
    Task<TenantDto?> GetCurrentTenantAsync();

    /// <summary>
    /// Sets the tenant context for the current request.
    /// </summary>
    Task SetTenantContextAsync(Guid tenantId);

    /// <summary>
    /// Gets tenant by ID.
    /// </summary>
    Task<TenantDto?> GetTenantByIdAsync(Guid tenantId);
}




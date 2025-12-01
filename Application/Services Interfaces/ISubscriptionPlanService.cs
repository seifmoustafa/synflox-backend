using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.PlanDto;
using Application.DTOs.Subscriptions;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for Subscription Plan management
/// Single Responsibility: Subscription Plans only
/// SYNFLOX ID Encryption Rule Compliant
/// </summary>
public interface ISubscriptionPlanService
{
    Task<PlanDto> CreateAsync(CreateSubscriptionPlanDto dto);
    Task<PlanDto?> GetByIdAsync(PlanIdRequest request);
    Task<PlanDetailsDto?> GetDetailsAsync(PlanIdRequest request);
    Task<(IEnumerable<PlanDto> Plans, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<PlanDto?> UpdateAsync(PlanIdRequest request, UpdatePlanDto dto);
    Task<bool> DeleteAsync(PlanIdRequest request);
    
    /// <summary>
    /// Get all free tier plans (for fallback plan dropdown)
    /// </summary>
    Task<IEnumerable<PlanDto>> GetFreeTierPlansAsync();
    
    /// <summary>
    /// Get all plans that can be set as parent (excludes plan itself and descendants to prevent circular refs)
    /// </summary>
    Task<IEnumerable<PlanDto>> GetAvailableParentPlansAsync(Guid? excludePlanId = null);
}

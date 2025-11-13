using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanDto dto);
    Task<SubscriptionPlanDto?> GetByIdAsync(PlanIdRequest request);
    Task<PlanDetailsDto?> GetDetailsAsync(PlanIdRequest request);
    Task<(IEnumerable<SubscriptionPlanDto> Plans, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<SubscriptionPlanDto?> UpdateAsync(PlanIdRequest request, UpdateSubscriptionPlanDto dto);
    Task<bool> DeleteAsync(PlanIdRequest request);
}

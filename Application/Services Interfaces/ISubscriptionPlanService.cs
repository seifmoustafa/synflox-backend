using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for Subscription Plan management
/// Single Responsibility: Subscription Plans only
/// </summary>
public interface ISubscriptionPlanService
{
    Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanDto dto);
    Task<SubscriptionPlanDto?> GetByIdAsync(Guid id);
    Task<PlanDetailsDto?> GetDetailsAsync(Guid id);
    Task<(IEnumerable<SubscriptionPlanDto> Plans, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<SubscriptionPlanDto?> UpdateAsync(Guid id, UpdateSubscriptionPlanDto dto);
    Task<bool> DeleteAsync(Guid id);
}

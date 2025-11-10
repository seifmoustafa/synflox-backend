using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing subscription plans.
/// </summary>
public interface ISubscriptionPlanService
{
    /// <summary>
    /// Creates a new subscription plan.
    /// </summary>
    Task<SubscriptionPlanDto> CreatePlanAsync(CreateSubscriptionPlanRequest request);

    /// <summary>
    /// Gets all subscription plans.
    /// </summary>
    Task<(IEnumerable<SubscriptionPlanDto> Plans, PaginationMetadata Meta)> GetAllPlansAsync(
        bool? isActive = null,
        int page = 1,
        int pageSize = 10,
        string? search = null);

    /// <summary>
    /// Gets a subscription plan by ID.
    /// </summary>
    Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid id);

    /// <summary>
    /// Updates a subscription plan.
    /// </summary>
    Task<SubscriptionPlanDto?> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanRequest request);

    /// <summary>
    /// Deletes a subscription plan (soft delete).
    /// </summary>
    Task<bool> DeletePlanAsync(Guid id);

    /// <summary>
    /// Updates the project-modules included in a subscription plan.
    /// </summary>
    Task<SubscriptionPlanDto> UpdatePlanProjectModulesAsync(Guid planId, UpdatePlanProjectModulesDto dto);

    /// <summary>
    /// Gets all project-modules included in a subscription plan.
    /// </summary>
    Task<(IEnumerable<PlanProjectModuleDto> PlanProjectModules, PaginationMetadata Meta)> GetPlanProjectModulesAsync(
        Guid planId,
        int page = 1,
        int pageSize = 10);
}


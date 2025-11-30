using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Entitlements;
using Application.Services;
using AutoMapper;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing subscription entitlements
/// </summary>
public class EntitlementService : IEntitlementService
{
    private readonly ISubscriptionEntitlementRepository _entitlementRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IModuleRepository _moduleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EntitlementService> _logger;
    private readonly ILocalizationService _localizer;
    private readonly ICurrentUserService _currentUserService;

    private const string MATRIX_CACHE_KEY = "EntitlementMatrix_{0}";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(24);

    public EntitlementService(
        ISubscriptionEntitlementRepository entitlementRepo,
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        IProjectRepository projectRepo,
        IModuleRepository moduleRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<EntitlementService> logger,
        ILocalizationService localizer,
        ICurrentUserService currentUserService)
    {
        _entitlementRepo = entitlementRepo;
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _projectRepo = projectRepo;
        _moduleRepo = moduleRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
    }

    #region CRUD Operations

    public async Task<IEnumerable<SubscriptionEntitlementDto>> GetSubscriptionEntitlementsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var entitlements = await _entitlementRepo.GetBySubscriptionWithDetailsAsync(subscriptionId, cancellationToken);
        return _mapper.Map<IEnumerable<SubscriptionEntitlementDto>>(entitlements);
    }

    public async Task<SubscriptionEntitlementDto?> GetEntitlementByIdAsync(
        Guid entitlementId,
        CancellationToken cancellationToken = default)
    {
        var entitlement = await _entitlementRepo.GetByIdAsync(entitlementId, null, cancellationToken);
        if (entitlement == null || entitlement.IsDeleted)
            return null;

        return _mapper.Map<SubscriptionEntitlementDto>(entitlement);
    }

    public async Task<SubscriptionEntitlementDto> GrantEntitlementAsync(
        GrantEntitlementRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Granting entitlement to subscription {SubscriptionId}", request.SubscriptionId);

        // Map request → CreateEntitlementDto (handles decryption via AutoMapper)
        var createDto = _mapper.Map<CreateEntitlementDto>(request);
        
        // Verify subscription exists
        var subscription = await _subscriptionRepo.GetByIdAsync(createDto.SubscriptionId, null, cancellationToken);
        if (subscription == null)
            throw new ArgumentException(_localizer["Subscription.NotFound"]);

        // Apply CRUD flags based on access level if not explicitly set
        var (canCreate, canRead, canUpdate, canDelete, canExport) = GetCrudFlagsForAccessLevel(request.AccessLevel);
        createDto.CanCreate = request.CanCreate ?? canCreate;
        createDto.CanRead = request.CanRead ?? canRead;
        createDto.CanUpdate = request.CanUpdate ?? canUpdate;
        createDto.CanDelete = request.CanDelete ?? canDelete;
        createDto.CanExport = request.CanExport ?? canExport;
        createDto.GrantedByAdminId = _currentUserService.UserId;

        // Map CreateEntitlementDto → Entity (handles ID generation and timestamps via AutoMapper)
        var entitlement = _mapper.Map<SubscriptionEntitlement>(createDto);

        await _entitlementRepo.AddAsync(entitlement);
        await IncrementVersionAsync(createDto.SubscriptionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Entitlement {EntitlementId} granted to subscription {SubscriptionId}", 
            entitlement.Id, createDto.SubscriptionId);

        return _mapper.Map<SubscriptionEntitlementDto>(entitlement);
    }

    public async Task<SubscriptionEntitlementDto> UpdateEntitlementAsync(
        UpdateEntitlementRequest request,
        CancellationToken cancellationToken = default)
    {
        // Decrypt entitlement ID
        var entitlementId = _mapper.Map<Guid>(request);

        var entitlement = await _entitlementRepo.GetByIdAsync(entitlementId, null, cancellationToken);
        if (entitlement == null || entitlement.IsDeleted)
            throw new ArgumentException(_localizer["Entitlement.NotFound"]);

        // Update fields if provided
        if (request.AccessLevel.HasValue)
        {
            entitlement.AccessLevel = request.AccessLevel.Value;
            var (c, r, u, d, e) = GetCrudFlagsForAccessLevel(request.AccessLevel.Value);
            entitlement.CanCreate = request.CanCreate ?? c;
            entitlement.CanRead = request.CanRead ?? r;
            entitlement.CanUpdate = request.CanUpdate ?? u;
            entitlement.CanDelete = request.CanDelete ?? d;
            entitlement.CanExport = request.CanExport ?? e;
        }
        else
        {
            if (request.CanCreate.HasValue) entitlement.CanCreate = request.CanCreate.Value;
            if (request.CanRead.HasValue) entitlement.CanRead = request.CanRead.Value;
            if (request.CanUpdate.HasValue) entitlement.CanUpdate = request.CanUpdate.Value;
            if (request.CanDelete.HasValue) entitlement.CanDelete = request.CanDelete.Value;
            if (request.CanExport.HasValue) entitlement.CanExport = request.CanExport.Value;
        }

        if (request.Features != null)
            entitlement.Features = JsonSerializer.Serialize(request.Features);
        if (request.UsageLimits != null)
            entitlement.UsageLimits = JsonSerializer.Serialize(request.UsageLimits);
        if (request.Icon != null)
            entitlement.Icon = request.Icon;
        if (request.DisplayInMenu.HasValue)
            entitlement.DisplayInMenu = request.DisplayInMenu.Value;
        if (request.UpgradeCta != null)
            entitlement.UpgradeCta = request.UpgradeCta;
        if (request.UpgradeUrl != null)
            entitlement.UpgradeUrl = request.UpgradeUrl;
        if (request.ExpiresAt.HasValue)
            entitlement.ExpiresAt = request.ExpiresAt == DateTime.MinValue ? null : request.ExpiresAt;
        if (request.Notes != null)
            entitlement.Notes = request.Notes;
        if (request.IsActive.HasValue)
            entitlement.IsActive = request.IsActive.Value;

        entitlement.UpdatedTimestamp = DateTime.Now;

        await _entitlementRepo.UpdateAsync(entitlement);
        await IncrementVersionAsync(entitlement.SubscriptionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Entitlement {EntitlementId} updated", entitlementId);

        return _mapper.Map<SubscriptionEntitlementDto>(entitlement);
    }

    public async Task RevokeEntitlementAsync(
        RevokeEntitlementRequest request,
        CancellationToken cancellationToken = default)
    {
        var entitlementId = _mapper.Map<Guid>(request);

        var entitlement = await _entitlementRepo.GetByIdAsync(entitlementId, null, cancellationToken);
        if (entitlement == null)
            throw new ArgumentException(_localizer["Entitlement.NotFound"]);

        if (request.HardDelete)
        {
            await _entitlementRepo.DeleteAsync(entitlement.Id);
        }
        else
        {
            entitlement.IsDeleted = true;
            entitlement.IsActive = false;
            entitlement.Notes = $"{entitlement.Notes}\n[Revoked: {request.Reason}]".Trim();
            entitlement.DeletedTimestamp = DateTime.Now;
            await _entitlementRepo.UpdateAsync(entitlement);
        }

        await IncrementVersionAsync(entitlement.SubscriptionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Entitlement {EntitlementId} revoked. Reason: {Reason}", entitlementId, request.Reason);
    }

    #endregion

    #region Simplified Grant Operations

    public async Task<SubscriptionEntitlementDto> GrantProjectAccessAsync(
        GrantProjectAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        return await GrantEntitlementAsync(new GrantEntitlementRequest
        {
            SubscriptionId = request.SubscriptionId,
            ProjectId = request.ProjectId,
            ModuleId = null,
            GrantType = EntitlementGrantType.FullProject,
            AccessLevel = request.AccessLevel,
            Source = EntitlementSource.AdminGrant,
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes
        }, cancellationToken);
    }

    public async Task<SubscriptionEntitlementDto> GrantModuleAccessAsync(
        GrantModuleAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        return await GrantEntitlementAsync(new GrantEntitlementRequest
        {
            SubscriptionId = request.SubscriptionId,
            ProjectId = request.ProjectId,
            ModuleId = request.ModuleId,
            GrantType = EntitlementGrantType.SpecificModules,
            AccessLevel = request.AccessLevel,
            Source = EntitlementSource.AdminGrant,
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes
        }, cancellationToken);
    }

    public async Task<SubscriptionEntitlementDto> GrantStandaloneModuleAccessAsync(
        GrantStandaloneModuleRequest request,
        CancellationToken cancellationToken = default)
    {
        return await GrantEntitlementAsync(new GrantEntitlementRequest
        {
            SubscriptionId = request.SubscriptionId,
            ProjectId = null,
            ModuleId = request.ModuleId,
            GrantType = EntitlementGrantType.StandaloneModule,
            AccessLevel = request.AccessLevel,
            Source = EntitlementSource.AdminGrant,
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes
        }, cancellationToken);
    }

    #endregion

    #region Bulk Operations

    public async Task<int> CopyPlanEntitlementsToSubscriptionAsync(
        Guid subscriptionId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Copying plan {PlanId} entitlements to subscription {SubscriptionId}", 
            planId, subscriptionId);

        var plan = await _planRepo.GetWithProjectsAndModulesAsync(planId, cancellationToken);
        if (plan == null)
            throw new ArgumentException(_localizer["Plan.NotFound"]);

        var createDtos = new List<CreateEntitlementDto>();

        // Add project entitlements using factory method
        foreach (var planProject in plan.PlanProjects)
        {
            createDtos.Add(CreateEntitlementDto.ForFullProject(
                subscriptionId, 
                planProject.ProjectId, 
                EntitlementSource.PlanDefault));
        }

        // Add standalone module entitlements
        foreach (var planModule in plan.PlanModules)
        {
            // Check if this module is already covered by a project
            var isStandalone = !plan.PlanProjects.Any(pp => 
                planModule.Module?.ProjectModules?.Any(pm => pm.ProjectId == pp.ProjectId) == true);

            if (isStandalone)
            {
                createDtos.Add(CreateEntitlementDto.ForStandaloneModule(
                    subscriptionId, 
                    planModule.ModuleId, 
                    EntitlementSource.PlanDefault));
            }
        }

        // Map all DTOs to entities using AutoMapper
        var entitlements = createDtos.Select(dto => _mapper.Map<SubscriptionEntitlement>(dto)).ToList();

        await _entitlementRepo.BulkInsertAsync(entitlements, cancellationToken);
        await IncrementVersionAsync(subscriptionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Copied {Count} entitlements from plan {PlanId} to subscription {SubscriptionId}", 
            entitlements.Count, planId, subscriptionId);

        return entitlements.Count;
    }

    public async Task<int> AddUpgradeEntitlementsAsync(
        Guid subscriptionId,
        Guid newPlanId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding upgrade entitlements from plan {PlanId} to subscription {SubscriptionId}", 
            newPlanId, subscriptionId);

        var newPlan = await _planRepo.GetWithProjectsAndModulesAsync(newPlanId, cancellationToken);
        if (newPlan == null)
            throw new ArgumentException(_localizer["Plan.NotFound"]);

        var existingEntitlements = await _entitlementRepo.GetBySubscriptionIdAsync(subscriptionId, cancellationToken);
        var existingProjectIds = existingEntitlements.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).ToHashSet();
        var existingModuleIds = existingEntitlements.Where(e => e.ModuleId.HasValue).Select(e => e.ModuleId!.Value).ToHashSet();

        var createDtos = new List<CreateEntitlementDto>();

        // Add new project entitlements using factory method
        foreach (var planProject in newPlan.PlanProjects)
        {
            if (!existingProjectIds.Contains(planProject.ProjectId))
            {
                createDtos.Add(CreateEntitlementDto.ForFullProject(
                    subscriptionId, 
                    planProject.ProjectId, 
                    EntitlementSource.Upgrade));
            }
        }

        // Add new standalone module entitlements using factory method
        foreach (var planModule in newPlan.PlanModules)
        {
            if (!existingModuleIds.Contains(planModule.ModuleId))
            {
                createDtos.Add(CreateEntitlementDto.ForStandaloneModule(
                    subscriptionId, 
                    planModule.ModuleId, 
                    EntitlementSource.Upgrade));
            }
        }

        if (createDtos.Any())
        {
            // Map all DTOs to entities using AutoMapper
            var newEntitlements = createDtos.Select(dto => _mapper.Map<SubscriptionEntitlement>(dto)).ToList();
            
            await _entitlementRepo.BulkInsertAsync(newEntitlements, cancellationToken);
            await IncrementVersionAsync(subscriptionId, cancellationToken);
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("Added {Count} upgrade entitlements to subscription {SubscriptionId}", 
            createDtos.Count, subscriptionId);

        return createDtos.Count;
    }

    public async Task<int> ReplaceEntitlementsAsync(
        Guid subscriptionId,
        Guid newPlanId,
        bool keepCustomGrants = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Replacing entitlements for subscription {SubscriptionId} with plan {PlanId}", 
            subscriptionId, newPlanId);

        // Deactivate existing entitlements (optionally keep custom grants)
        var existingEntitlements = await _entitlementRepo.GetBySubscriptionIdAsync(subscriptionId, cancellationToken);
        foreach (var entitlement in existingEntitlements)
        {
            if (keepCustomGrants && entitlement.IsCustom)
                continue;

            entitlement.IsActive = false;
            entitlement.UpdatedTimestamp = DateTime.Now;
            await _entitlementRepo.UpdateAsync(entitlement);
        }

        // Copy new plan entitlements
        var count = await CopyPlanEntitlementsToSubscriptionAsync(subscriptionId, newPlanId, cancellationToken);

        return count;
    }

    public async Task<int> DowngradeToFallbackAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
            throw new ArgumentException(_localizer["Subscription.NotFound"]);

        var fallbackPlanId = subscription.FallbackPlanId ?? subscription.Plan?.DefaultFallbackPlanId;
        if (!fallbackPlanId.HasValue)
        {
            _logger.LogWarning("No fallback plan configured for subscription {SubscriptionId}", subscriptionId);
            return 0;
        }

        return await ReplaceEntitlementsAsync(subscriptionId, fallbackPlanId.Value, true, cancellationToken);
    }

    public async Task<int> RevokeAllEntitlementsAsync(
        RevokeAllEntitlementsRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscriptionId = _mapper.Map<Guid>(new { Id = request.SubscriptionId });
        var count = await _entitlementRepo.DeactivateAllForSubscriptionAsync(subscriptionId, cancellationToken);
        await IncrementVersionAsync(subscriptionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Revoked all {Count} entitlements for subscription {SubscriptionId}. Reason: {Reason}", 
            count, subscriptionId, request.Reason);

        return count;
    }

    public async Task<int> BulkRevokeAsync(
        BulkRevokeRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscriptionIds = new HashSet<Guid>();
        foreach (var encryptedId in request.EntitlementIds)
        {
            var entitlement = await _entitlementRepo.GetByIdAsync(encryptedId, null, cancellationToken);
            if (entitlement != null)
            {
                entitlement.IsActive = false;
                entitlement.IsDeleted = true;
                entitlement.Notes = $"{entitlement.Notes}\n[Bulk Revoked: {request.Reason}]".Trim();
                entitlement.DeletedTimestamp = DateTime.Now;
                await _entitlementRepo.UpdateAsync(entitlement);
                subscriptionIds.Add(entitlement.SubscriptionId);
            }
        }

        foreach (var subscriptionId in subscriptionIds)
        {
            await IncrementVersionAsync(subscriptionId, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Bulk revoked {Count} entitlements. Reason: {Reason}", 
            request.EntitlementIds.Count, request.Reason);

        return request.EntitlementIds.Count;
    }

    #endregion

    #region Access Checking

    /// <summary>
    /// Check access using encrypted request DTO (from API)
    /// </summary>
    public async Task<AccessCheckResult> CheckAccessAsync(
        AccessCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        // Decrypt subscription ID via AutoMapper
        var subscriptionId = _mapper.Map<Guid>(request);
        
        // Call internal method with decrypted Guids
        return await CheckAccessInternalAsync(
            subscriptionId, 
            request.ProjectId, 
            request.ModuleId, 
            request.Feature, 
            request.Operation, 
            cancellationToken);
    }

    /// <summary>
    /// Internal access check with already-decrypted Guids
    /// </summary>
    private async Task<AccessCheckResult> CheckAccessInternalAsync(
        Guid subscriptionId,
        Guid? projectId,
        Guid? moduleId,
        string? feature,
        string? operation,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null, cancellationToken);
        
        if (subscription == null)
            return AccessCheckResult.Denied("Subscription not found");

        // Check subscription-level access mode
        var effectiveMode = await GetEffectiveAccessModeAsync(subscriptionId, cancellationToken);
        if (effectiveMode == SubscriptionAccessMode.Blocked)
            return AccessCheckResult.Denied("Subscription is blocked", effectiveMode);

        // Get specific entitlement
        SubscriptionEntitlement? entitlement = null;
        
        if (projectId.HasValue && moduleId.HasValue)
        {
            entitlement = await _entitlementRepo.GetBySubscriptionProjectAndModuleAsync(
                subscriptionId, projectId.Value, moduleId.Value, cancellationToken);
        }
        else if (projectId.HasValue)
        {
            entitlement = await _entitlementRepo.GetBySubscriptionAndProjectAsync(
                subscriptionId, projectId.Value, cancellationToken);
        }
        else if (moduleId.HasValue)
        {
            entitlement = await _entitlementRepo.GetBySubscriptionAndModuleAsync(
                subscriptionId, moduleId.Value, cancellationToken);
        }

        if (entitlement == null)
            return AccessCheckResult.Denied("No entitlement found for this resource");

        // Check operation if specified
        if (!string.IsNullOrEmpty(operation))
        {
            var isAllowed = operation.ToUpper() switch
            {
                "GET" => entitlement.CanRead,
                "POST" => entitlement.CanCreate,
                "PUT" => entitlement.CanUpdate,
                "DELETE" => entitlement.CanDelete,
                "EXPORT" => entitlement.CanExport,
                _ => false
            };

            if (!isAllowed)
                return AccessCheckResult.Denied($"Operation '{operation}' not allowed");
        }

        // Apply subscription-level access mode restrictions
        if (effectiveMode == SubscriptionAccessMode.ReadOnly)
            return AccessCheckResult.ReadOnly(subscription.AccessRestrictionMessage);
        
        if (effectiveMode == SubscriptionAccessMode.ExportOnly)
            return AccessCheckResult.ExportOnly(subscription.AccessRestrictionMessage);

        return AccessCheckResult.Granted(
            entitlement.AccessLevel,
            GetAllowedOperations(entitlement),
            ParseFeatures(entitlement.Features));
    }

    public async Task<bool> HasProjectAccessAsync(
        Guid subscriptionId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _entitlementRepo.HasProjectAccessAsync(subscriptionId, projectId, cancellationToken);
    }

    public async Task<bool> HasModuleAccessAsync(
        Guid subscriptionId,
        Guid projectId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        return await _entitlementRepo.HasModuleAccessAsync(subscriptionId, projectId, moduleId, cancellationToken);
    }

    public async Task<bool> HasStandaloneModuleAccessAsync(
        Guid subscriptionId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        return await _entitlementRepo.HasStandaloneModuleAccessAsync(subscriptionId, moduleId, cancellationToken);
    }

    public async Task<bool> IsOperationAllowedAsync(
        Guid subscriptionId,
        Guid? projectId,
        Guid? moduleId,
        string operation,
        CancellationToken cancellationToken = default)
    {
        // This method receives DECRYPTED Guids, so call internal check directly
        var result = await CheckAccessInternalAsync(subscriptionId, projectId, moduleId, null, operation, cancellationToken);
        return result.HasAccess;
    }

    public async Task<BulkAccessCheckResult> BulkCheckAccessAsync(
        BulkAccessCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        // Decrypt subscription ID via AutoMapper
        var subscriptionId = _mapper.Map<Guid>(request);
        var accessMode = await GetEffectiveAccessModeAsync(subscriptionId, cancellationToken);

        var results = new List<AccessCheckResultItem>();
        foreach (var check in request.Checks)
        {
            // Use internal method with decrypted subscription ID
            var result = await CheckAccessInternalAsync(
                subscriptionId,
                check.ProjectId,
                check.ModuleId,
                check.Feature,
                check.Operation,
                cancellationToken);

            // Map result to result item
            results.Add(new AccessCheckResultItem
            {
                ProjectId = check.ProjectId,
                ModuleId = check.ModuleId,
                Feature = check.Feature,
                Operation = check.Operation,
                HasAccess = result.HasAccess,
                AccessLevel = result.AccessLevel,
                DenialReason = result.DenialReason
            });
        }

        // Return with ORIGINAL encrypted SubscriptionId (not the decrypted one)
        return new BulkAccessCheckResult
        {
            SubscriptionId = request.SubscriptionId, // Keep encrypted ID for response
            AccessMode = accessMode,
            Results = results
        };
    }

    #endregion

    #region Entitlement Matrix

    public async Task<EntitlementMatrixDto> BuildEntitlementMatrixAsync(
        Guid subscriptionId,
        bool includeUpgrades = true,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
            throw new ArgumentException(_localizer["Subscription.NotFound"]);

        var entitlements = await _entitlementRepo.GetBySubscriptionWithDetailsAsync(subscriptionId, cancellationToken);

        // Map subscription → EntitlementMatrixDto using AutoMapper
        var matrix = _mapper.Map<EntitlementMatrixDto>(subscription);
        
        // Set access mode (requires async call, can't do in mapper)
        matrix.AccessMode = await GetEffectiveAccessModeAsync(subscriptionId, cancellationToken);

        // Group by project
        var projectEntitlements = entitlements
            .Where(e => e.ProjectId.HasValue && e.ModuleId == null)
            .ToList();

        var moduleEntitlements = entitlements
            .Where(e => e.ModuleId.HasValue)
            .ToList();

        var standaloneModules = entitlements
            .Where(e => e.ProjectId == null && e.ModuleId.HasValue)
            .ToList();

        // Build project list
        foreach (var pe in projectEntitlements)
        {
            var projectDto = _mapper.Map<ProjectEntitlementDto>(pe);
            
            // Add modules for this project
            projectDto.Modules = moduleEntitlements
                .Where(m => m.ProjectId == pe.ProjectId)
                .Select(m => _mapper.Map<ModuleEntitlementDto>(m))
                .ToList();

            matrix.Projects.Add(projectDto);
        }

        // Add standalone modules
        matrix.StandaloneModules = standaloneModules
            .Select(m => _mapper.Map<ModuleEntitlementDto>(m))
            .ToList();

        // Cache the matrix
        var cacheKey = string.Format(MATRIX_CACHE_KEY, subscriptionId);
        _cache.Set(cacheKey, matrix, CACHE_DURATION);

        return matrix;
    }

    public async Task<EntitlementMatrixDto> GetCachedEntitlementMatrixAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(MATRIX_CACHE_KEY, subscriptionId);
        
        if (_cache.TryGetValue(cacheKey, out EntitlementMatrixDto? cached) && cached != null)
        {
            // Check version
            var currentVersion = await GetEntitlementsVersionAsync(subscriptionId, cancellationToken);
            if (cached.Version == currentVersion)
                return cached;
        }

        return await BuildEntitlementMatrixAsync(subscriptionId, true, cancellationToken);
    }

    #endregion

    #region Versioning

    public async Task<int> GetEntitlementsVersionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null, cancellationToken);
        return subscription?.EntitlementsVersion ?? 0;
    }

    public async Task<int> IncrementVersionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null, cancellationToken);
        if (subscription == null)
            return 0;

        subscription.EntitlementsVersion++;
        subscription.UpdatedTimestamp = DateTime.Now;
        await _subscriptionRepo.UpdateAsync(subscription);

        // Invalidate cache
        await InvalidateCacheAsync(subscriptionId, cancellationToken);

        return subscription.EntitlementsVersion;
    }

    public Task InvalidateCacheAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(MATRIX_CACHE_KEY, subscriptionId);
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    #endregion

    #region Access Mode Management

    public async Task<SubscriptionAccessMode> GetEffectiveAccessModeAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
            return SubscriptionAccessMode.Blocked;

        // Active and not expired = stored access mode (usually Full)
        if (subscription.IsActive && !subscription.IsExpired)
            return subscription.AccessMode;

        // Check grace period
        if (subscription.Plan != null)
        {
            var graceEnd = subscription.ExpiryDateUtc.AddDays(subscription.Plan.GracePeriodDays);
            var now = DateTime.Now;
            if (now > subscription.ExpiryDateUtc && now <= graceEnd)
                return SubscriptionAccessMode.GracePeriod;
        }

        // Check export deadline
        if (subscription.ExportDeadlineUtc.HasValue && DateTime.Now <= subscription.ExportDeadlineUtc.Value)
            return SubscriptionAccessMode.ExportOnly;

        // Has fallback plan = ReadOnly
        if (subscription.FallbackPlanId.HasValue)
            return SubscriptionAccessMode.ReadOnly;

        return SubscriptionAccessMode.Blocked;
    }

    public async Task UpdateAccessModeAsync(
        Guid subscriptionId,
        SubscriptionAccessMode newMode,
        string? restrictionMessage = null,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null, cancellationToken);
        if (subscription == null)
            throw new ArgumentException(_localizer["Subscription.NotFound"]);

        subscription.AccessMode = newMode;
        subscription.AccessRestrictionMessage = restrictionMessage;
        subscription.UpdatedTimestamp = DateTime.Now;

        await _subscriptionRepo.UpdateAsync(subscription);
        await IncrementVersionAsync(subscriptionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Access mode for subscription {SubscriptionId} updated to {Mode}", 
            subscriptionId, newMode);
    }

    public async Task<bool> IsInGracePeriodAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var mode = await GetEffectiveAccessModeAsync(subscriptionId, cancellationToken);
        return mode == SubscriptionAccessMode.GracePeriod;
    }

    public async Task<int?> GetDaysRemainingInCurrentModeAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
            return null;

        var mode = await GetEffectiveAccessModeAsync(subscriptionId, cancellationToken);
        var now = DateTime.Now;

        return mode switch
        {
            SubscriptionAccessMode.Full when subscription.Plan?.DurationType == PlanDurationType.Lifetime => null,
            SubscriptionAccessMode.Full => Math.Max(0, (int)(subscription.ExpiryDateUtc - now).TotalDays),
            SubscriptionAccessMode.GracePeriod when subscription.Plan != null => 
                Math.Max(0, (int)(subscription.ExpiryDateUtc.AddDays(subscription.Plan.GracePeriodDays) - now).TotalDays),
            SubscriptionAccessMode.ExportOnly when subscription.ExportDeadlineUtc.HasValue => 
                Math.Max(0, (int)(subscription.ExportDeadlineUtc.Value - now).TotalDays),
            SubscriptionAccessMode.ReadOnly => null, // Indefinite
            _ => null
        };
    }

    #endregion

    #region Utilities

    public (bool CanCreate, bool CanRead, bool CanUpdate, bool CanDelete, bool CanExport) GetCrudFlagsForAccessLevel(
        EntitlementAccessLevel level)
    {
        return level switch
        {
            EntitlementAccessLevel.Full => (true, true, true, true, true),
            EntitlementAccessLevel.ReadOnly => (false, true, false, false, true),
            EntitlementAccessLevel.ExportOnly => (false, true, false, false, true),
            EntitlementAccessLevel.Blocked => (false, false, false, false, false),
            _ => (false, false, false, false, false)
        };
    }

    public async Task<SubscriptionEntitlementDto> CloneEntitlementAsync(
        Guid sourceEntitlementId,
        Guid targetSubscriptionId,
        EntitlementSource source,
        CancellationToken cancellationToken = default)
    {
        var sourceEntitlement = await _entitlementRepo.GetByIdAsync(sourceEntitlementId, null, cancellationToken);
        if (sourceEntitlement == null)
            throw new ArgumentException(_localizer["Entitlement.NotFound"]);

        // Map source entity → CreateEntitlementDto (copies all properties)
        var createDto = _mapper.Map<CreateEntitlementDto>(sourceEntitlement);
        
        // Modify for new subscription
        createDto.SubscriptionId = targetSubscriptionId;
        createDto.Source = source;
        createDto.IsCustom = false;
        
        // Map CreateEntitlementDto → new Entity (AutoMapper sets Id, timestamps, etc.)
        var clone = _mapper.Map<SubscriptionEntitlement>(createDto);

        await _entitlementRepo.AddAsync(clone);
        await IncrementVersionAsync(targetSubscriptionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SubscriptionEntitlementDto>(clone);
    }

    private static List<string> GetAllowedOperations(SubscriptionEntitlement e)
    {
        var ops = new List<string>();
        if (e.CanRead) ops.Add("GET");
        if (e.CanCreate) ops.Add("POST");
        if (e.CanUpdate) ops.Add("PUT");
        if (e.CanDelete) ops.Add("DELETE");
        if (e.CanExport) ops.Add("EXPORT");
        return ops;
    }

    private static List<string> ParseFeatures(string? featuresJson)
    {
        if (string.IsNullOrEmpty(featuresJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(featuresJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    #endregion
}

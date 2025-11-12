using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Application.Services;
using AutoMapper;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing subscription lifecycle operations
/// Single Responsibility: Subscription management only
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IOutboxEventRepository _outboxRepo;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        ICompanyRepository companyRepo,
        IOutboxEventRepository outboxRepo,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _companyRepo = companyRepo;
        _outboxRepo = outboxRepo;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto)
    {
        // Validate company exists
        var company = await _companyRepo.GetByIdAsync(dto.CompanyId, null);
        if (company == null)
            throw new NotFoundException(_localizer["Company.NotFound"]);

        // Validate plan exists and get details
        var plan = await _planRepo.GetWithDetailsAsync(dto.PlanId);
        if (plan == null)
            throw new NotFoundException(_localizer["Plan.NotFound"]);

        // Validate trial request
        if (dto.StartWithTrial && !plan.AllowTrial)
            throw new PlanTrialNotAllowedException(_localizer["Plan.TrialNotAllowed"]);

        // Get price for selected currency
        var price = await _planRepo.GetPriceAsync(dto.PlanId, dto.Currency);
        if (!price.HasValue)
            throw new BadRequestException(_localizer["Plan.PriceNotAvailableForCurrency"]);

        // Calculate subscription dates
        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CompanyId = dto.CompanyId,
            PlanId = dto.PlanId,
            StartDateUtc = now,
            IsTrial = dto.StartWithTrial,
            IsActive = true,
            IsExpired = false,
            AutoRenew = dto.AutoRenew ?? plan.AutoRenew,
            Currency = dto.Currency,
            Amount = price.Value,
            StatusReason = dto.StartWithTrial ? "Trial started" : "Subscription activated"
        };

        // Calculate expiry based on trial or paid
        if (dto.StartWithTrial)
        {
            subscription.ExpiryDateUtc = now.AddDays(plan.TrialDurationDays!.Value);
        }
        else
        {
            subscription.ExpiryDateUtc = now.AddMonths(plan.DurationMonths);
        }

        // Handle scheduled next plan (deferred upgrade)
        if (dto.NextPlanId.HasValue)
        {
            var nextPlan = await _planRepo.GetByIdAsync(dto.NextPlanId.Value, null);
            if (nextPlan == null)
                throw new NotFoundException(_localizer["Plan.NotFound"]);

            subscription.NextPlanId = dto.NextPlanId;
            subscription.NextPlanStartDateUtc = dto.NextPlanStartDateUtc ?? subscription.ExpiryDateUtc.AddSeconds(1);

            if (subscription.NextPlanStartDateUtc < subscription.ExpiryDateUtc)
                throw new InvalidNextPlanScheduleException(_localizer["Subscription.NextPlanBeforeExpiry"]);
        }

        // Check for overlapping active subscriptions (same company + plan)
        var hasOverlap = await _subscriptionRepo.HasOverlappingActiveSubscriptionAsync(
            subscription.CompanyId,
            subscription.PlanId,
            subscription.StartDateUtc,
            subscription.ExpiryDateUtc);

        if (hasOverlap)
            throw new OverlappingActiveSubscriptionException(_localizer["Subscription.OverlappingActive"]);

        // Create subscription
        await _subscriptionRepo.AddAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Create outbox event for email notification
        await CreateOutboxEventAsync(
            dto.StartWithTrial ? SubscriptionEventType.TrialStarted : SubscriptionEventType.Created,
            subscription.CompanyId,
            subscription.Id,
            new
            {
                CompanyName = company.Name,
                CompanyEmail = company.ContactEmail,
                PlanName = plan.Name,
                ExpiryDate = subscription.ExpiryDateUtc,
                IsTrial = subscription.IsTrial,
                Currency = subscription.Currency.ToString(),
                Amount = subscription.Amount
            });

        var result = await _subscriptionRepo.GetWithDetailsAsync(subscription.Id);
        var subscriptionDto = _mapper.Map<SubscriptionDto>(result!);
        SetLicenseKeyIfSuperAdmin(subscriptionDto, result!);
        return subscriptionDto;
    }

    public async Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid id)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(id);
        if (subscription == null) return null;
        
        var dto = _mapper.Map<SubscriptionDto>(subscription);
        SetLicenseKeyIfSuperAdmin(dto, subscription);
        return dto;
    }

    public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid companyId)
    {
        var subscription = await _subscriptionRepo.GetActiveByCompanyIdAsync(companyId);
        if (subscription == null) return null;
        
        var dto = _mapper.Map<SubscriptionDto>(subscription);
        SetLicenseKeyIfSuperAdmin(dto, subscription);
        return dto;
    }

    public async Task<IEnumerable<SubscriptionDto>> GetCompanySubscriptionsAsync(Guid companyId)
    {
        var subscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(companyId);
        var dtos = _mapper.Map<IEnumerable<SubscriptionDto>>(subscriptions).ToList();
        
        // Set license key visibility for each subscription
        for (int i = 0; i < dtos.Count; i++)
        {
            SetLicenseKeyIfSuperAdmin(dtos[i], subscriptions.ElementAt(i));
        }
        
        return dtos;
    }

    public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            return null;

        var statusDto = _mapper.Map<SubscriptionStatusDto>(subscription);

        // Compute localized status message
        statusDto.StatusMessage = ComputeStatusMessage(subscription);

        return statusDto;
    }

    public async Task<SubscriptionDto> RenewSubscriptionAsync(Guid subscriptionId, RenewSubscriptionDto dto)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        var plan = subscription.Plan;
        var company = subscription.Company;

        Subscription newSubscription;

        if (dto.RenewStrategy == "ExtendInPlace")
        {
            // Extend current subscription (not recommended but supported)
            subscription.ExpiryDateUtc = subscription.ExpiryDateUtc.AddMonths(plan.DurationMonths);
            subscription.AutoRenew = dto.NewAutoRenew ?? subscription.AutoRenew;
            subscription.StatusReason = "Extended in place";

            if (dto.NextPlanId.HasValue)
            {
                subscription.NextPlanId = dto.NextPlanId;
                subscription.NextPlanStartDateUtc = dto.NextPlanStartDateUtc ?? subscription.ExpiryDateUtc.AddSeconds(1);
            }

            await _subscriptionRepo.UpdateAsync(subscription);
            await _unitOfWork.SaveChangesAsync();

            newSubscription = subscription;
        }
        else // CreateFollowUp (preferred)
        {
            var startDate = subscription.IsExpired ? DateTime.UtcNow : subscription.ExpiryDateUtc.AddSeconds(1);

            newSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                CompanyId = subscription.CompanyId,
                PlanId = subscription.PlanId,
                StartDateUtc = startDate,
                ExpiryDateUtc = startDate.AddMonths(plan.DurationMonths),
                IsActive = true,
                IsExpired = false,
                IsTrial = false,
                AutoRenew = dto.NewAutoRenew ?? subscription.AutoRenew,
                Currency = subscription.Currency,
                Amount = subscription.Amount,
                ParentSubscriptionId = subscription.Id,
                StatusReason = "Renewed"
            };

            if (dto.NextPlanId.HasValue)
            {
                newSubscription.NextPlanId = dto.NextPlanId;
                newSubscription.NextPlanStartDateUtc = dto.NextPlanStartDateUtc ?? newSubscription.ExpiryDateUtc.AddSeconds(1);
            }

            await _subscriptionRepo.AddAsync(newSubscription);
            await _unitOfWork.SaveChangesAsync();
        }

        // Create outbox event
        await CreateOutboxEventAsync(
            SubscriptionEventType.Renewed,
            company.Id,
            newSubscription.Id,
            new
            {
                CompanyName = company.Name,
                CompanyEmail = company.ContactEmail,
                PlanName = plan.Name,
                NewExpiryDate = newSubscription.ExpiryDateUtc
            });

        var result = await _subscriptionRepo.GetWithDetailsAsync(newSubscription.Id);
        var renewedDto = _mapper.Map<SubscriptionDto>(result!);
        SetLicenseKeyIfSuperAdmin(renewedDto, result!);
        return renewedDto;
    }

    public async Task<UpgradeResponseDto> UpgradeSubscriptionAsync(Guid subscriptionId, UpgradeSubscriptionDto dto)
    {
        var oldSubscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (oldSubscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        var newPlan = await _planRepo.GetWithDetailsAsync(dto.NewPlanId);
        if (newPlan == null)
            throw new NotFoundException(_localizer["Plan.NotFound"]);

        var company = oldSubscription.Company;
        var oldPlan = oldSubscription.Plan;

        // Determine effective upgrade mode
        var mode = dto.Mode == "DefaultFromPolicy"
            ? (oldSubscription.UpgradePolicyOverride ?? oldPlan.UpgradePolicy).ToString()
            : dto.Mode;

        // Get new plan price
        var newPrice = await _planRepo.GetPriceAsync(dto.NewPlanId, oldSubscription.Currency);
        if (!newPrice.HasValue)
            throw new BadRequestException(_localizer["Plan.PriceNotAvailableForCurrency"]);

        Subscription? newSubscription = null;
        ProrationSuggestionDto? proration = null;

        var now = DateTime.UtcNow;

        switch (mode)
        {
            case "FullReplace":
                // Terminate current immediately, start new with full duration
                oldSubscription.IsActive = false;
                oldSubscription.IsExpired = true;
                oldSubscription.StatusReason = $"Upgraded to {newPlan.Name} (FullReplace)";
                await _subscriptionRepo.UpdateAsync(oldSubscription);

                newSubscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    CompanyId = oldSubscription.CompanyId,
                    PlanId = dto.NewPlanId,
                    StartDateUtc = now,
                    ExpiryDateUtc = now.AddMonths(newPlan.DurationMonths),
                    IsActive = true,
                    IsExpired = false,
                    IsTrial = false,
                    AutoRenew = dto.NewAutoRenew ?? newPlan.AutoRenew,
                    Currency = oldSubscription.Currency,
                    Amount = newPrice.Value,
                    ParentSubscriptionId = oldSubscription.Id,
                    StatusReason = $"Upgraded from {oldPlan.Name} (FullReplace)"
                };
                break;

            case "Prorated":
                // Calculate proration
                var remainingDays = (oldSubscription.ExpiryDateUtc - now).Days;
                var oldPlanDays = (oldSubscription.ExpiryDateUtc - oldSubscription.StartDateUtc).Days;
                var oldDailyRate = oldSubscription.Amount / oldPlanDays;
                var suggestedCredit = oldDailyRate * remainingDays;

                var newPlanDays = newPlan.DurationMonths * 30; // Approximate
                var newDailyRate = newPrice.Value / newPlanDays;
                var newFullCharge = newPrice.Value;
                var netDue = newFullCharge - suggestedCredit;

                proration = new ProrationSuggestionDto
                {
                    RemainingDays = remainingDays,
                    OldDailyRate = Math.Round(oldDailyRate, 2),
                    SuggestedCredit = Math.Round(suggestedCredit, 2),
                    NewDailyRate = Math.Round(newDailyRate, 2),
                    SuggestedCharge = Math.Round(newFullCharge, 2),
                    NetDue = Math.Round(netDue, 2),
                    CurrencyCode = oldSubscription.Currency.ToString()
                };

                // Terminate current, start new
                oldSubscription.IsActive = false;
                oldSubscription.IsExpired = true;
                oldSubscription.StatusReason = $"Upgraded to {newPlan.Name} (Prorated)";
                await _subscriptionRepo.UpdateAsync(oldSubscription);

                newSubscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    CompanyId = oldSubscription.CompanyId,
                    PlanId = dto.NewPlanId,
                    StartDateUtc = now,
                    ExpiryDateUtc = now.AddMonths(newPlan.DurationMonths),
                    IsActive = true,
                    IsExpired = false,
                    IsTrial = false,
                    AutoRenew = dto.NewAutoRenew ?? newPlan.AutoRenew,
                    Currency = oldSubscription.Currency,
                    Amount = netDue,
                    ParentSubscriptionId = oldSubscription.Id,
                    StatusReason = $"Upgraded from {oldPlan.Name} (Prorated)"
                };
                break;

            case "Deferred":
                // Schedule upgrade for later
                oldSubscription.NextPlanId = dto.NewPlanId;
                oldSubscription.NextPlanStartDateUtc = oldSubscription.ExpiryDateUtc.AddSeconds(1);
                oldSubscription.StatusReason = $"Upgrade to {newPlan.Name} scheduled (Deferred)";
                await _subscriptionRepo.UpdateAsync(oldSubscription);
                break;

            default:
                throw new UpgradeConflictException(_localizer["Subscription.InvalidUpgradeMode"]);
        }

        if (newSubscription != null)
        {
            await _subscriptionRepo.AddAsync(newSubscription);
        }

        await _unitOfWork.SaveChangesAsync();

        // Create outbox event
        await CreateOutboxEventAsync(
            SubscriptionEventType.Upgraded,
            company.Id,
            newSubscription?.Id ?? oldSubscription.Id,
            new
            {
                CompanyName = company.Name,
                CompanyEmail = company.ContactEmail,
                OldPlanName = oldPlan.Name,
                NewPlanName = newPlan.Name,
                Mode = mode,
                NewExpiryDate = newSubscription?.ExpiryDateUtc
            });

        var response = new UpgradeResponseDto
        {
            Mode = mode,
            OldPlanName = oldPlan.Name,
            NewPlanName = newPlan.Name,
            OldWindow = new SubscriptionWindowDto
            {
                StartDateUtc = oldSubscription.StartDateUtc,
                ExpiryDateUtc = oldSubscription.ExpiryDateUtc,
                DurationDays = (oldSubscription.ExpiryDateUtc - oldSubscription.StartDateUtc).Days
            },
            NewWindow = newSubscription != null ? new SubscriptionWindowDto
            {
                StartDateUtc = newSubscription.StartDateUtc,
                ExpiryDateUtc = newSubscription.ExpiryDateUtc,
                DurationDays = (newSubscription.ExpiryDateUtc - newSubscription.StartDateUtc).Days
            } : null,
            ProrationSuggestion = proration,
            NewSubscription = GetMappedSubscriptionDto(newSubscription)
        };

        return response;
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        subscription.IsActive = false;
        subscription.IsExpired = true;
        subscription.NextPlanId = null;
        subscription.NextPlanStartDateUtc = null;
        subscription.StatusReason = "Canceled by admin";

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Create outbox event
        await CreateOutboxEventAsync(
            SubscriptionEventType.Canceled,
            subscription.CompanyId,
            subscription.Id,
            new
            {
                CompanyName = subscription.Company.Name,
                CompanyEmail = subscription.Company.ContactEmail,
                PlanName = subscription.Plan.Name
            });

        return true;
    }

    #region Helper Methods

    private string ComputeStatusMessage(Subscription subscription)
    {
        var now = DateTime.UtcNow;

        if (subscription.IsExpired)
        {
            return _localizer["Subscription.Status.Expired"];
        }

        if (!subscription.IsActive)
        {
            return _localizer["Subscription.Status.Suspended"];
        }

        if (subscription.IsTrial)
        {
            var daysRemaining = (subscription.ExpiryDateUtc - now).Days;
            return string.Format(_localizer["Subscription.Status.TrialActive"], daysRemaining);
        }

        var graceEnd = subscription.ExpiryDateUtc.AddDays(subscription.Plan.GracePeriodDays);
        if (now > subscription.ExpiryDateUtc && now <= graceEnd)
        {
            var graceDaysRemaining = (graceEnd - now).Days;
            return string.Format(_localizer["Subscription.Status.GracePeriod"], graceDaysRemaining);
        }

        return _localizer["Subscription.Status.Active"];
    }

    private async Task CreateOutboxEventAsync(
        SubscriptionEventType eventType,
        Guid companyId,
        Guid subscriptionId,
        object payload)
    {
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            CompanyId = companyId,
            SubscriptionId = subscriptionId,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAtUtc = DateTime.UtcNow,
            IsProcessed = false,
            AttemptCount = 0
        };

        await _outboxRepo.AddAsync(outboxEvent);
    }

    /// <summary>
    /// Sets license key visibility in DTO based on user role
    /// Only SuperAdmin can see license keys
    /// </summary>
    private void SetLicenseKeyIfSuperAdmin(SubscriptionDto dto, Subscription subscription)
    {
        if (_currentUserService.AdminTypeName == "SuperAdmin")
        {
            dto.OfflineLicenseKey = subscription.OfflineLicenseKey;
        }
    }

    /// <summary>
    /// Helper method to map subscription to DTO with license key visibility
    /// </summary>
    private SubscriptionDto? GetMappedSubscriptionDto(Subscription? subscription)
    {
        if (subscription == null) return null;
        
        var dto = _mapper.Map<SubscriptionDto>(subscription);
        SetLicenseKeyIfSuperAdmin(dto, subscription);
        return dto;
    }

    #endregion
}

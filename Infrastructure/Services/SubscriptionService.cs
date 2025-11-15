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
using Microsoft.Extensions.Logging;

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
    private readonly IClientTokenService _clientTokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        ICompanyRepository companyRepo,
        IOutboxEventRepository outboxRepo,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IClientTokenService clientTokenService,
        IEmailService emailService,
        ILogger<SubscriptionService> logger)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _companyRepo = companyRepo;
        _outboxRepo = outboxRepo;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _clientTokenService = clientTokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto)
    {
        // Use AutoMapper to convert DTO to entity (handles ID decryption automatically)
        var subscription = _mapper.Map<Subscription>(dto);
        
        // Validate company exists (using decrypted ID from mapping)
        var company = await _companyRepo.GetByIdAsync(subscription.CompanyId, null);
        if (company == null)
            throw new NotFoundException(_localizer["Company.NotFound"]);

        // Validate plan exists and get details (using decrypted ID from mapping)
        var plan = await _planRepo.GetWithDetailsAsync(subscription.PlanId);
        if (plan == null)
            throw new NotFoundException(_localizer["Plan.NotFound"]);

        // Validate trial request
        if (dto.StartWithTrial && !plan.AllowTrial)
            throw new PlanTrialNotAllowedException(_localizer["Plan.TrialNotAllowed"]);

        // Get price for selected currency
        var price = await _planRepo.GetPriceAsync(subscription.PlanId, dto.Currency);
        if (!price.HasValue)
            throw new BadRequestException(_localizer["Plan.PriceNotAvailableForCurrency"]);

        // Set additional properties not handled by AutoMapper
        var now = DateTime.UtcNow;
        subscription.Id = Guid.NewGuid();
        subscription.StartDateUtc = now;
        subscription.IsTrial = dto.StartWithTrial;
        subscription.IsActive = true;
        subscription.IsExpired = false;
        subscription.AutoRenew = dto.AutoRenew ?? plan.AutoRenew;
        subscription.Currency = dto.Currency;
        subscription.Amount = price.Value;
        subscription.StatusReason = dto.StartWithTrial ? "Trial started" : "Subscription activated";

        // Calculate expiry based on trial or paid
        if (dto.StartWithTrial)
        {
            subscription.ExpiryDateUtc = now.AddDays(plan.TrialDurationDays!.Value);
        }
        else
        {
            subscription.ExpiryDateUtc = now.AddMonths(plan.DurationMonths);
        }

        // Handle scheduled next plan (deferred upgrade) - NextPlanId already decrypted by AutoMapper
        if (subscription.NextPlanId.HasValue)
        {
            var nextPlan = await _planRepo.GetByIdAsync(subscription.NextPlanId.Value, null);
            if (nextPlan == null)
                throw new NotFoundException(_localizer["Plan.NotFound"]);

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
        
        // Auto-generate client access token for new subscription
        try
        {
            await _clientTokenService.AutoGenerateTokenForSubscriptionAsync(subscription.Id);
            _logger.LogInformation("Auto-generated client access token for subscription {SubscriptionId}", subscription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-generate client token for subscription {SubscriptionId}", subscription.Id);
            // Don't fail the subscription creation if token generation fails
        }
        
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

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string reason, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        subscription.IsActive = false;
        subscription.IsExpired = true;
        subscription.StatusReason = reason ?? "Canceled by administrator";

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendSubscriptionCanceledEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            language);

        // Create outbox event for cancellation
        await CreateOutboxEventAsync(
            SubscriptionEventType.Canceled,
            subscription.CompanyId,
            subscription.Id,
            new
            {
                CompanyName = subscription.Company.Name,
                CompanyEmail = subscription.Company.ContactEmail,
                PlanName = subscription.Plan.Name,
                Reason = reason
            });

        return true;
    }

    public async Task<bool> SuspendSubscriptionAsync(Guid subscriptionId, string reason, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        if (!subscription.IsActive)
            throw new InvalidOperationException(_localizer["Subscription.AlreadyInactive"]);

        subscription.IsActive = false;
        subscription.StatusReason = reason ?? "Suspended by administrator";

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendSubscriptionSuspendedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            reason ?? "Suspended by administrator",
            language);

        // Create outbox event
        await CreateOutboxEventAsync(
            SubscriptionEventType.Suspended,
            subscription.CompanyId,
            subscription.Id,
            new
            {
                CompanyName = subscription.Company.Name,
                CompanyEmail = subscription.Company.ContactEmail,
                PlanName = subscription.Plan.Name,
                Reason = reason
            });

        return true;
    }

    public async Task<bool> ResumeSubscriptionAsync(Guid subscriptionId, string reason, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        if (subscription.IsActive)
            throw new InvalidOperationException(_localizer["Subscription.AlreadyActive"]);

        subscription.IsActive = true;
        subscription.IsExpired = false;
        subscription.StatusReason = reason ?? "Resumed by administrator";

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendSubscriptionResumedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            subscription.ExpiryDateUtc,
            language);

        // Create outbox event
        await CreateOutboxEventAsync(
            SubscriptionEventType.Resumed,
            subscription.CompanyId,
            subscription.Id,
            new
            {
                CompanyName = subscription.Company.Name,
                CompanyEmail = subscription.Company.ContactEmail,
                PlanName = subscription.Plan.Name,
                Reason = reason,
                ExpiryDate = subscription.ExpiryDateUtc
            });

        return true;
    }

    public async Task<bool> PauseSubscriptionAsync(Guid subscriptionId, string reason, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        if (!subscription.IsActive)
            throw new InvalidOperationException(_localizer["Subscription.NotActive"]);

        // Store the pause date to calculate remaining time later
        subscription.StatusReason = $"Paused: {reason ?? "Paused by administrator"}";
        // Note: In a full implementation, you'd want to add PausedAtUtc field to track pause time

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendSubscriptionPausedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            reason ?? "Paused by administrator",
            language);

        return true;
    }

    public async Task<bool> UnpauseSubscriptionAsync(Guid subscriptionId, string reason, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        subscription.StatusReason = reason ?? "Unpaused by administrator";
        // Note: In a full implementation, you'd calculate and adjust the expiry date based on pause duration

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendSubscriptionResumedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            subscription.ExpiryDateUtc,
            language);

        return true;
    }

    public async Task<bool> StopTrialAsync(Guid subscriptionId, string reason, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        if (!subscription.IsTrial)
            throw new InvalidOperationException(_localizer["Subscription.NotTrial"]);

        // Convert trial to paid subscription
        subscription.IsTrial = false;
        subscription.StatusReason = reason ?? "Trial converted to paid subscription";
        
        // Extend expiry to full plan duration from now
        var fullDuration = subscription.Plan.DurationMonths;
        subscription.ExpiryDateUtc = DateTime.UtcNow.AddMonths(fullDuration);

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendTrialStoppedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            subscription.ExpiryDateUtc,
            language);

        return true;
    }

    public async Task<SubscriptionDto> ExtendSubscriptionAsync(Guid subscriptionId, ExtendSubscriptionDto dto, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        var oldExpiryDate = subscription.ExpiryDateUtc;
        subscription.ExpiryDateUtc = subscription.ExpiryDateUtc.AddDays(dto.ExtensionDays);
        subscription.StatusReason = dto.Reason ?? $"Extended by {dto.ExtensionDays} days";

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification if requested
        if (dto.SendEmailNotification)
        {
            await _emailService.SendSubscriptionExtendedEmailAsync(
                subscription.Company.ContactEmail,
                subscription.Company.Name,
                subscription.Plan.Name,
                oldExpiryDate,
                subscription.ExpiryDateUtc,
                dto.ExtensionDays,
                language);
        }

        return GetMappedSubscriptionDto(subscription)!;
    }

    public async Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId, string reason, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        if (subscription.IsActive)
            throw new InvalidOperationException(_localizer["Subscription.AlreadyActive"]);

        subscription.IsActive = true;
        subscription.IsExpired = false;
        subscription.StatusReason = reason ?? "Reactivated by administrator";
        
        // Extend expiry if it's in the past
        if (subscription.ExpiryDateUtc <= DateTime.UtcNow)
        {
            subscription.ExpiryDateUtc = DateTime.UtcNow.AddMonths(subscription.Plan.DurationMonths);
        }

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendSubscriptionReactivatedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            subscription.ExpiryDateUtc,
            language);

        return true;
    }

    public async Task<IEnumerable<object>> GetSubscriptionHistoryAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        // Return audit trail - in a full implementation, you'd have a separate audit table
        return new List<object>
        {
            new
            {
                Action = "Created",
                Timestamp = subscription.CreatedTimestamp,
                Reason = "Subscription created",
                Details = new { subscription.PlanId, subscription.CompanyId }
            },
            new
            {
                Action = "Current Status",
                Timestamp = subscription.UpdatedTimestamp ?? subscription.CreatedTimestamp,
                Reason = subscription.StatusReason,
                Details = new { subscription.IsActive, subscription.IsExpired, subscription.IsTrial }
            }
        };
    }

    public async Task<object> GetSubscriptionAnalyticsAsync(Guid subscriptionId, DateTime? fromDate, DateTime? toDate)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;

        return new
        {
            SubscriptionId = subscriptionId,
            CompanyName = subscription.Company.Name,
            PlanName = subscription.Plan.Name,
            Period = new { From = from, To = to },
            Status = new
            {
                subscription.IsActive,
                subscription.IsExpired,
                subscription.IsTrial,
                DaysRemaining = subscription.IsActive ? (subscription.ExpiryDateUtc - DateTime.UtcNow).Days : 0
            },
            Usage = new
            {
                TotalDays = (to - from).Days,
                ActiveDays = subscription.IsActive ? (DateTime.UtcNow - subscription.StartDateUtc).Days : 0,
                UtilizationPercentage = subscription.IsActive ? 
                    Math.Round(((DateTime.UtcNow - subscription.StartDateUtc).Days / (double)(subscription.ExpiryDateUtc - subscription.StartDateUtc).Days) * 100, 2) : 0
            }
        };
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

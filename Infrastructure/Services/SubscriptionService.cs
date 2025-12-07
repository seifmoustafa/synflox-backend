using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Application.Services;
using Application.Services_Interfaces;
using AutoMapper;
using Domain.Entities.Subscriptions;
using Domain.Entities.Common;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Helpers;
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
    private readonly ISubscriptionHistoryRepository _historyRepo;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IClientTokenService _clientTokenService;
    private readonly IEmailService _emailService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly ICurrencyExchangeService _currencyExchangeService;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        ICompanyRepository companyRepo,
        IOutboxEventRepository outboxRepo,
        ISubscriptionHistoryRepository historyRepo,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IClientTokenService clientTokenService,
        IEmailService emailService,
        IActivityLogService activityLogService,
        ILogger<SubscriptionService> logger,
        ICurrencyExchangeService currencyExchangeService)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _companyRepo = companyRepo;
        _outboxRepo = outboxRepo;
        _historyRepo = historyRepo;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _clientTokenService = clientTokenService;
        _emailService = emailService;
        _activityLogService = activityLogService;
        _logger = logger;
        _currencyExchangeService = currencyExchangeService;
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto)
    {
        // Use AutoMapper to convert DTO to entity (handles ID decryption automatically)
        var subscription = _mapper.Map<Subscription>(dto);
        
        // Validate company exists (using decrypted ID from mapping)
        var company = await _companyRepo.GetByIdAsync(subscription.CompanyId, null);
        if (company == null)
            throw new NotFoundException(_localizer["Company.NotFound"]);
        
        // Validate company is active
        if (!company.IsActive)
            throw new BadRequestException(_localizer["Company.InactiveCannotSubscribe"]);

        // Validate plan exists and get details (using decrypted ID from mapping)
        var plan = await _planRepo.GetWithDetailsAsync(subscription.PlanId);
        if (plan == null)
            throw new NotFoundException(_localizer["Plan.NotFound"]);

        // Check if company already has an active subscription with the same plan
        var (existingSubscriptions, _) = await _subscriptionRepo.GetAllAsync(
            s => s.CompanyId == subscription.CompanyId 
                 && s.PlanId == subscription.PlanId 
                 && s.IsActive 
                 && !s.IsDeleted,
            null);
        if (existingSubscriptions.Any())
            throw new BadRequestException(_localizer["Subscription.DuplicatePlanNotAllowed"]);
        
        // Stricter check for lifetime plans - cannot have ANY subscription (even expired) of same plan
        if (plan.IsLifetimePlan)
        {
            var (anyExisting, _) = await _subscriptionRepo.GetAllAsync(
                s => s.CompanyId == subscription.CompanyId 
                     && s.PlanId == subscription.PlanId 
                     && !s.IsDeleted,
                null);
            if (anyExisting.Any())
                throw new BadRequestException(_localizer["Subscription.LifetimeAlreadyExists"]);
        }

        // Validate trial request
        if (dto.StartWithTrial && !plan.AllowTrial)
            throw new PlanTrialNotAllowedException(_localizer["Plan.TrialNotAllowed"]);

        // Handle free tier plans - no currency or price needed
        Currency? selectedCurrency = null;
        decimal price = 0;
        
        if (plan.IsFreeTier)
        {
            // Free tier: no payment required, use default currency or first available
            var firstPrice = plan.PlanPrices.FirstOrDefault();
            selectedCurrency = dto.Currency ?? firstPrice?.Currency ?? Currency.USD;
            price = 0; // Free plan always has 0 price
        }
        else
        {
            // Paid plan: validate currency and price
            selectedCurrency = dto.Currency;
            if (!selectedCurrency.HasValue)
            {
                // Auto-select first available currency from plan prices
                var firstPrice = plan.PlanPrices.FirstOrDefault();
                if (firstPrice == null)
                    throw new BadRequestException(_localizer["Plan.NoPricesAvailable"]);
                selectedCurrency = firstPrice.Currency;
            }

            // Get price for selected currency
            var priceResult = await _planRepo.GetPriceAsync(subscription.PlanId, selectedCurrency.Value);
            if (!priceResult.HasValue)
                throw new BadRequestException(_localizer["Plan.PriceNotAvailableForCurrency"]);
            price = priceResult.Value;
        }

        // Validate lifetime plan rules
        if (plan.IsLifetimePlan)
        {
            if (dto.StartWithTrial)
                throw new BadRequestException(_localizer["Plan.LifetimeCannotHaveTrial"]);
            
            if (dto.AutoRenew == true)
                throw new BadRequestException(_localizer["Plan.LifetimeCannotAutoRenew"]);
        }

        // Set additional properties not handled by AutoMapper
        var now = DateTime.UtcNow;
        subscription.Id = Guid.NewGuid();
        subscription.StartDateUtc = now;
        subscription.IsTrial = dto.StartWithTrial;
        subscription.IsActive = true;
        subscription.IsExpired = false;
        
        // Lifetime plans cannot auto-renew
        subscription.AutoRenew = plan.IsLifetimePlan ? false : (dto.AutoRenew ?? plan.AutoRenew);
        
        subscription.Currency = selectedCurrency!.Value;
        subscription.Amount = price;
        subscription.StatusReason = dto.StartWithTrial ? "Trial started" : 
                                    plan.IsLifetimePlan ? "Lifetime subscription activated" :
                                    plan.IsFreeTier ? "Free tier subscription activated" : 
                                    "Subscription activated";

        // Calculate expiry based on plan duration type
        if (dto.StartWithTrial)
        {
            subscription.ExpiryDateUtc = now.AddDays(plan.TrialDurationDays!.Value);
        }
        else
        {
            // Use PlanDurationHelper to calculate expiry based on DurationType
            subscription.ExpiryDateUtc = PlanDurationHelper.CalculateExpiryDate(now, plan.DurationType);
        }

        // Check for overlapping active subscriptions (same company + plan)
        // Skip overlap check for lifetime plans (they can coexist with time-based plans)
        if (!plan.IsLifetimePlan)
        {
            var hasOverlap = await _subscriptionRepo.HasOverlappingActiveSubscriptionAsync(
                subscription.CompanyId,
                subscription.PlanId,
                subscription.StartDateUtc,
                subscription.ExpiryDateUtc);

            if (hasOverlap)
                throw new OverlappingActiveSubscriptionException(_localizer["Subscription.OverlappingActive"]);
        }

        // Create subscription
        await _subscriptionRepo.AddAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogSubscriptionActivityAsync(
            ActivityActionType.Created,
            subscription.Id,
            $"{company.Name} - {plan.Name}",
            _currentUserService.UserId,
            null,
            dto.StartWithTrial ? "Trial subscription created" : "Subscription created");

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

    public async Task<(IEnumerable<SubscriptionDto> Items, PaginationMetadata Pagination)> GetAllSubscriptionsAsync(int page, int pageSize, string? search = null)
    {
        var (subscriptions, pagination) = await _subscriptionRepo.GetAllAsync(
            new[] { "Plan", "Company" }, // Include related data
            page,
            pageSize,
            search,
            default,
            s => s.Company.Name, s => s.Plan.Name); // Search by company name and plan name

        var dtos = subscriptions.Select(s => {
            var dto = _mapper.Map<SubscriptionDto>(s);
            // Set license key visibility based on role (handled by controller/auth)
            dto.OfflineLicenseKey = null; // Will be set by controller if user has permission
            return dto;
        });

        return (dtos, pagination);
    }

    public async Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid id, string? displayCurrency = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(id);
        if (subscription == null) return null;
        
        var dto = _mapper.Map<SubscriptionDto>(subscription);
        SetLicenseKeyIfSuperAdmin(dto, subscription);
        
        // Convert currency if display currency is specified
        if (!string.IsNullOrEmpty(displayCurrency))
        {
            dto = await ConvertSubscriptionCurrencyAsync(dto, displayCurrency);
        }
        
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

    public async Task<IEnumerable<SubscriptionDto>> GetCompanySubscriptionsAsync(Guid companyId, string? displayCurrency = null)
    {
        var subscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(companyId);
        var dtos = _mapper.Map<IEnumerable<SubscriptionDto>>(subscriptions).ToList();
        
        // Set license key visibility for each subscription
        for (int i = 0; i < dtos.Count; i++)
        {
            SetLicenseKeyIfSuperAdmin(dtos[i], subscriptions.ElementAt(i));
        }
        
        // Convert currency if display currency is specified
        if (!string.IsNullOrEmpty(displayCurrency))
        {
            for (int i = 0; i < dtos.Count; i++)
            {
                dtos[i] = await ConvertSubscriptionCurrencyAsync(dtos[i], displayCurrency);
            }
        }
        
        return dtos;
    }
    
    public async Task<(IEnumerable<SubscriptionDto> data, PaginationMetadata pagination)> GetCompanySubscriptionsPaginatedAsync(Guid companyId, int page, int pageSize)
    {
        // Get paginated subscriptions
        (IEnumerable<Subscription> subscriptions, PaginationMetadata meta) = await _subscriptionRepo.GetAllAsync(
            s => s.CompanyId == companyId && !s.IsDeleted,
            null,
            page,
            pageSize
        );
        
        // Order by CreatedTimestamp descending (newest first)
        var orderedSubscriptions = subscriptions.OrderByDescending(s => s.CreatedTimestamp).ToList();
        
        var dtos = _mapper.Map<IEnumerable<SubscriptionDto>>(orderedSubscriptions).ToList();
        
        // Set license key visibility for each subscription
        for (int i = 0; i < dtos.Count; i++)
        {
            SetLicenseKeyIfSuperAdmin(dtos[i], orderedSubscriptions.ElementAt(i));
        }
        
        return (dtos, meta);
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

        // Lifetime subscriptions cannot be renewed (already permanent)
        if (subscription.IsLifetime)
            throw new InvalidOperationException(_localizer["Subscription.LifetimeCannotRenew"]);

        var plan = subscription.Plan;
        var company = subscription.Company;

        // Store previous values for history
        var previousExpiryDate = subscription.ExpiryDateUtc;
        var previousStatus = subscription.IsActive ? LicenseStatus.Active : 
                            (subscription.IsExpired ? LicenseStatus.Expired : LicenseStatus.Suspended);

        // SINGLE STRATEGY: Update existing subscription in place
        // Calculate new expiry from current expiry (if active) or from now (if expired)
        var baseDate = subscription.IsExpired ? DateTime.UtcNow : subscription.ExpiryDateUtc;
        subscription.ExpiryDateUtc = PlanDurationHelper.CalculateExpiryDate(baseDate, plan.DurationType);
        subscription.AutoRenew = dto.NewAutoRenew ?? subscription.AutoRenew;
        subscription.IsActive = true;
        subscription.IsExpired = false;
        subscription.StatusReason = dto.Reason ?? "Renewed";

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history - all changes tracked here
        await RecordHistoryAsync(subscriptionId, "Renewed", 
            previousStatus: previousStatus,
            newStatus: LicenseStatus.Active,
            previousExpiryDate: previousExpiryDate, 
            newExpiryDate: subscription.ExpiryDateUtc,
            reason: dto.Reason ?? "Subscription renewed");
        
        await _unitOfWork.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogSubscriptionActivityAsync(
            ActivityActionType.Renewed,
            subscription.Id,
            $"{company.Name} - {plan.Name}",
            _currentUserService.UserId,
            null,
            $"Subscription renewed until {subscription.ExpiryDateUtc:d}");

        // Send renewal email notification
        await _emailService.SendSubscriptionRenewedEmailAsync(
            company.ContactEmail,
            company.Name,
            plan.Name,
            subscription.ExpiryDateUtc,
            dto.Reason);

        // Create outbox event
        await CreateOutboxEventAsync(
            SubscriptionEventType.Renewed,
            company.Id,
            subscription.Id,
            new
            {
                CompanyName = company.Name,
                CompanyEmail = company.ContactEmail,
                PlanName = plan.Name,
                PreviousExpiryDate = previousExpiryDate,
                NewExpiryDate = subscription.ExpiryDateUtc
            });

        var result = await _subscriptionRepo.GetWithDetailsAsync(subscription.Id);
        var renewedDto = _mapper.Map<SubscriptionDto>(result!);
        SetLicenseKeyIfSuperAdmin(renewedDto, result!);
        return renewedDto;
    }

    public async Task<UpgradeResponseDto> UpgradeSubscriptionAsync(Guid subscriptionId, UpgradeSubscriptionDto dto)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);
        
        // Validate subscription is active and not expired
        if (!subscription.IsActive || subscription.IsExpired)
            throw new BadRequestException(_localizer["Subscription.CannotUpgradeInactive"]);

        // Decrypt NewPlanId using AutoMapper (SYNFLOX ID ENCRYPTION RULE)
        var decryptedNewPlanId = _mapper.Map<Guid>(new UpgradeNewPlanIdRequest { NewPlanId = dto.NewPlanId });

        var newPlan = await _planRepo.GetWithDetailsAsync(decryptedNewPlanId);
        if (newPlan == null)
            throw new NotFoundException(_localizer["Plan.NotFound"]);

        var company = subscription.Company;
        var oldPlan = subscription.Plan;

        // Determine effective upgrade mode (use plan's policy if not specified)
        var mode = dto.Mode == "DefaultFromPolicy"
            ? oldPlan.UpgradePolicy.ToString()
            : dto.Mode;

        // Get new plan price
        var newPrice = await _planRepo.GetPriceAsync(decryptedNewPlanId, subscription.Currency);
        if (!newPrice.HasValue)
            throw new BadRequestException(_localizer["Plan.PriceNotAvailableForCurrency"]);

        ProrationSuggestionDto? proration = null;
        var now = DateTime.UtcNow;

        // Store previous values for history
        var previousPlanId = subscription.PlanId;
        var previousPlanName = oldPlan.Name;
        var previousExpiryDate = subscription.ExpiryDateUtc;
        var previousAmount = subscription.Amount;
        var previousStartDate = subscription.StartDateUtc;

        switch (mode)
        {
            case "FullReplace":
                // SINGLE STRATEGY: Update existing subscription with new plan
                subscription.PlanId = decryptedNewPlanId;
                subscription.StartDateUtc = now;
                subscription.ExpiryDateUtc = PlanDurationHelper.CalculateExpiryDate(now, newPlan.DurationType);
                subscription.IsActive = true;
                subscription.IsExpired = false;
                subscription.AutoRenew = dto.NewAutoRenew ?? newPlan.AutoRenew;
                subscription.Amount = newPrice.Value;
                subscription.StatusReason = $"Upgraded from {oldPlan.Name} to {newPlan.Name}";
                break;

            case "Prorated":
                // Calculate proration
                var remainingDays = Math.Max(0, (subscription.ExpiryDateUtc - now).Days);
                var oldPlanDays = Math.Max(1, (subscription.ExpiryDateUtc - subscription.StartDateUtc).Days);
                var oldDailyRate = subscription.Amount / oldPlanDays;
                var suggestedCredit = oldDailyRate * remainingDays;

                var newPlanDays = PlanDurationHelper.GetApproximateDays(newPlan.DurationType);
                var newDailyRate = newPrice.Value / newPlanDays;
                var newFullCharge = newPrice.Value;
                var netDue = Math.Max(0, newFullCharge - suggestedCredit);

                proration = new ProrationSuggestionDto
                {
                    RemainingDays = remainingDays,
                    OldDailyRate = Math.Round(oldDailyRate, 2),
                    SuggestedCredit = Math.Round(suggestedCredit, 2),
                    NewDailyRate = Math.Round(newDailyRate, 2),
                    SuggestedCharge = Math.Round(newFullCharge, 2),
                    NetDue = Math.Round(netDue, 2),
                    CurrencyCode = subscription.Currency.ToString()
                };

                // Update existing subscription with new plan (prorated)
                subscription.PlanId = decryptedNewPlanId;
                subscription.StartDateUtc = now;
                subscription.ExpiryDateUtc = PlanDurationHelper.CalculateExpiryDate(now, newPlan.DurationType);
                subscription.IsActive = true;
                subscription.IsExpired = false;
                subscription.AutoRenew = dto.NewAutoRenew ?? newPlan.AutoRenew;
                subscription.Amount = netDue;
                subscription.StatusReason = $"Upgraded from {oldPlan.Name} to {newPlan.Name} (Prorated)";
                break;

            case "Deferred":
                // Create a new subscription for the scheduled upgrade
                var scheduledSubscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    CompanyId = subscription.CompanyId,
                    PlanId = decryptedNewPlanId,
                    StartDateUtc = subscription.ExpiryDateUtc.AddSeconds(1),
                    ExpiryDateUtc = PlanDurationHelper.CalculateExpiryDate(subscription.ExpiryDateUtc.AddSeconds(1), newPlan.DurationType),
                    IsActive = false, // Not active until scheduled date
                    IsTrial = false,
                    IsExpired = false,
                    AutoRenew = dto.NewAutoRenew ?? newPlan.AutoRenew,
                    Currency = subscription.Currency,
                    Amount = newPrice.Value,
                    ParentSubscriptionId = subscription.Id,
                    StatusReason = "Scheduled upgrade - pending activation",
                    AccessMode = SubscriptionAccessMode.None // Not active yet
                };
                
                await _subscriptionRepo.AddAsync(scheduledSubscription);
                
                // Link current subscription to the scheduled one
                subscription.NextSubscriptionId = scheduledSubscription.Id;
                subscription.NextSubscriptionActivationDateUtc = scheduledSubscription.StartDateUtc;
                subscription.StatusReason = $"Upgrade to {newPlan.Name} scheduled for {scheduledSubscription.StartDateUtc:d}";
                break;

            default:
                throw new UpgradeConflictException(_localizer["Subscription.InvalidUpgradeMode"]);
        }

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history - tracks the upgrade with all details
        await RecordHistoryAsync(subscriptionId, $"Upgraded ({mode})", 
            previousStatus: LicenseStatus.Active,
            newStatus: LicenseStatus.Active,
            previousExpiryDate: previousExpiryDate, 
            newExpiryDate: subscription.ExpiryDateUtc,
            reason: $"Plan changed from {previousPlanName} to {newPlan.Name}",
            notes: proration != null ? $"Credit: {proration.SuggestedCredit}, Net Due: {proration.NetDue}" : null);
        
        await _unitOfWork.SaveChangesAsync();

        // Send upgrade email notification
        await _emailService.SendSubscriptionUpgradedEmailAsync(
            company.ContactEmail,
            company.Name,
            oldPlan.Name,
            newPlan.Name,
            subscription.ExpiryDateUtc,
            null);

        // Create outbox event
        await CreateOutboxEventAsync(
            SubscriptionEventType.Upgraded,
            company.Id,
            subscription.Id,
            new
            {
                CompanyName = company.Name,
                CompanyEmail = company.ContactEmail,
                OldPlanName = oldPlan.Name,
                NewPlanName = newPlan.Name,
                Mode = mode,
                PreviousExpiryDate = previousExpiryDate,
                NewExpiryDate = subscription.ExpiryDateUtc
            });

        // Build response
        var result = await _subscriptionRepo.GetWithDetailsAsync(subscription.Id);
        var response = new UpgradeResponseDto
        {
            Message = string.Format(_localizer["Subscription.UpgradeSuccess"], oldPlan.Name, newPlan.Name),
            Mode = mode,
            OldPlanName = oldPlan.Name,
            NewPlanName = newPlan.Name,
            OldWindow = new SubscriptionWindowDto
            {
                StartDateUtc = previousStartDate,
                ExpiryDateUtc = previousExpiryDate,
                DurationDays = (previousExpiryDate - previousStartDate).Days
            },
            NewWindow = new SubscriptionWindowDto
            {
                StartDateUtc = subscription.StartDateUtc,
                ExpiryDateUtc = subscription.ExpiryDateUtc,
                DurationDays = (subscription.ExpiryDateUtc - subscription.StartDateUtc).Days
            },
            ProrationSuggestion = proration,
            NewSubscription = _mapper.Map<SubscriptionDto>(result!)
        };

        return response;
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string reason, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        var previousStatus = subscription.IsActive ? LicenseStatus.Active : 
                           subscription.IsExpired ? LicenseStatus.Expired : LicenseStatus.Suspended;

        subscription.IsActive = false;
        subscription.IsExpired = true;
        subscription.StatusReason = reason ?? "Canceled by administrator";
        
        // Revoke offline license key when subscription is canceled (security)
        subscription.OfflineLicenseKey = null;
        subscription.LicenseKeyGeneratedAt = null;

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history
        await RecordHistoryAsync(subscriptionId, "Cancelled", previousStatus, LicenseStatus.Expired, 
            reason: reason ?? "Canceled by administrator");
        
        await _unitOfWork.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogSubscriptionActivityAsync(
            ActivityActionType.Cancelled,
            subscription.Id,
            $"{subscription.Company.Name} - {subscription.Plan.Name}",
            _currentUserService.UserId,
            null,
            reason ?? "Subscription cancelled");

        // Send email notification
        await _emailService.SendSubscriptionCancelledEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            reason,
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
        
        // Revoke offline license key when subscription is suspended (security)
        subscription.OfflineLicenseKey = null;
        subscription.LicenseKeyGeneratedAt = null;

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history
        await RecordHistoryAsync(subscriptionId, "Suspended", LicenseStatus.Active, LicenseStatus.Suspended, 
            reason: reason ?? "Suspended by administrator");
        
        await _unitOfWork.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogSubscriptionActivityAsync(
            ActivityActionType.Suspended,
            subscription.Id,
            $"{subscription.Company.Name} - {subscription.Plan.Name}",
            _currentUserService.UserId,
            null,
            reason ?? "Subscription suspended");

        // Send email notification
        await _emailService.SendSubscriptionSuspendedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            reason,
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
        
        // Record history
        await RecordHistoryAsync(subscriptionId, "Resumed", LicenseStatus.Suspended, LicenseStatus.Active, 
            reason: reason ?? "Resumed by administrator");
        
        await _unitOfWork.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogSubscriptionActivityAsync(
            ActivityActionType.Resumed,
            subscription.Id,
            $"{subscription.Company.Name} - {subscription.Plan.Name}",
            _currentUserService.UserId,
            null,
            reason ?? "Subscription resumed");

        // Send email notification with reason
        await _emailService.SendSubscriptionResumedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            reason,
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

        if (subscription.IsPaused)
            throw new InvalidOperationException(_localizer["Subscription.AlreadyPaused"]);

        // Lifetime subscriptions cannot be paused (no timer to freeze)
        if (subscription.IsLifetime)
            throw new InvalidOperationException(_localizer["Subscription.LifetimeCannotPause"]);

        // Calculate and store remaining days
        var now = DateTime.UtcNow;
        var remainingDays = Math.Max(0, (subscription.ExpiryDateUtc - now).Days);

        // Set pause state - timer is now frozen
        subscription.IsPaused = true;
        subscription.PausedAtUtc = now;
        subscription.RemainingDaysWhenPaused = remainingDays;
        subscription.StatusReason = reason ?? "Paused by administrator";

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history
        await RecordHistoryAsync(subscriptionId, "Paused", LicenseStatus.Active, null, 
            reason: reason ?? "Paused by administrator",
            notes: $"Remaining days frozen: {remainingDays}");
        
        await _unitOfWork.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogSubscriptionActivityAsync(
            ActivityActionType.Updated,
            subscription.Id,
            $"{subscription.Company.Name} - {subscription.Plan.Name}",
            _currentUserService.UserId,
            null,
            $"Subscription paused with {remainingDays} days remaining");

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

        if (!subscription.IsPaused)
            throw new InvalidOperationException(_localizer["Subscription.NotPaused"]);

        var now = DateTime.UtcNow;
        var oldExpiryDate = subscription.ExpiryDateUtc;
        
        // Restore remaining days from when it was paused
        // This effectively extends the expiry by the pause duration
        if (subscription.RemainingDaysWhenPaused.HasValue)
        {
            subscription.ExpiryDateUtc = now.AddDays(subscription.RemainingDaysWhenPaused.Value);
        }

        // Clear pause state
        subscription.IsPaused = false;
        var pauseDuration = subscription.PausedAtUtc.HasValue 
            ? (now - subscription.PausedAtUtc.Value).Days 
            : 0;
        subscription.PausedAtUtc = null;
        subscription.RemainingDaysWhenPaused = null;
        subscription.StatusReason = reason ?? "Unpaused by administrator";

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history with expiry date change
        await RecordHistoryAsync(subscriptionId, "Unpaused", null, LicenseStatus.Active,
            previousExpiryDate: oldExpiryDate,
            newExpiryDate: subscription.ExpiryDateUtc,
            reason: reason ?? "Unpaused by administrator",
            notes: $"Paused for {pauseDuration} days, expiry extended");
        
        await _unitOfWork.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogSubscriptionActivityAsync(
            ActivityActionType.Updated,
            subscription.Id,
            $"{subscription.Company.Name} - {subscription.Plan.Name}",
            _currentUserService.UserId,
            null,
            $"Subscription unpaused, expiry extended to {subscription.ExpiryDateUtc:d}");

        // Send email notification
        await _emailService.SendSubscriptionResumedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            reason,
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
        
        // Extend expiry to full plan duration from now using PlanDurationHelper
        var newExpiryDate = PlanDurationHelper.CalculateExpiryDate(DateTime.UtcNow, subscription.Plan.DurationType);
        var oldExpiryDate = subscription.ExpiryDateUtc;
        subscription.ExpiryDateUtc = newExpiryDate;

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history
        await RecordHistoryAsync(subscriptionId, "Trial Stopped", null, LicenseStatus.Active, 
            previousExpiryDate: oldExpiryDate, newExpiryDate: newExpiryDate,
            reason: reason ?? "Trial converted to paid subscription");
        
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendTrialStoppedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            reason,
            language);

        return true;
    }

    public async Task<SubscriptionDto> ExtendSubscriptionAsync(Guid subscriptionId, ExtendSubscriptionDto dto, string? language = null)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        // Lifetime subscriptions cannot be extended (already infinite)
        if (subscription.IsLifetime)
            throw new InvalidOperationException(_localizer["Subscription.LifetimeCannotExtend"]);

        var oldExpiryDate = subscription.ExpiryDateUtc;
        subscription.ExpiryDateUtc = subscription.ExpiryDateUtc.AddDays(dto.ExtensionDays);
        subscription.StatusReason = dto.Reason ?? $"Extended by {dto.ExtensionDays} days";

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history
        await RecordHistoryAsync(subscriptionId, "Extended", null, null, 
            previousExpiryDate: oldExpiryDate, newExpiryDate: subscription.ExpiryDateUtc,
            reason: dto.Reason ?? $"Extended by {dto.ExtensionDays} days");
        
        await _unitOfWork.SaveChangesAsync();

        // Send email notification if requested
        if (dto.SendEmailNotification)
        {
            await _emailService.SendSubscriptionExtendedEmailAsync(
                subscription.Company.ContactEmail,
                subscription.Company.Name,
                subscription.Plan.Name,
                subscription.ExpiryDateUtc,
                dto.ExtensionDays,
                dto.Reason,
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

        var previousStatus = subscription.IsExpired ? LicenseStatus.Expired : LicenseStatus.Suspended;
        
        subscription.IsActive = true;
        subscription.IsExpired = false;
        subscription.StatusReason = reason ?? "Reactivated by administrator";
        
        // Extend expiry if it's in the past (but not for lifetime plans)
        if (!subscription.IsLifetime && subscription.ExpiryDateUtc <= DateTime.UtcNow)
        {
            subscription.ExpiryDateUtc = PlanDurationHelper.CalculateExpiryDate(DateTime.UtcNow, subscription.Plan.DurationType);
        }

        await _subscriptionRepo.UpdateAsync(subscription);
        
        // Record history
        await RecordHistoryAsync(subscriptionId, "Reactivated", previousStatus, LicenseStatus.Active, 
            reason: reason ?? "Reactivated by administrator");
        
        await _unitOfWork.SaveChangesAsync();

        // Send email notification
        await _emailService.SendSubscriptionReactivatedEmailAsync(
            subscription.Company.ContactEmail,
            subscription.Company.Name,
            subscription.Plan.Name,
            reason,
            language);

        return true;
    }

    public async Task<IEnumerable<object>> GetSubscriptionHistoryAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        // Get actual history from database
        var historyEntries = await _historyRepo.GetBySubscriptionIdAsync(subscriptionId);
        
        return historyEntries.Select(h => new
        {
            Action = h.Action,
            Timestamp = h.CreatedTimestamp,
            Reason = h.Reason,
            Notes = h.Notes,
            PreviousStatus = h.PreviousStatus?.ToString(),
            NewStatus = h.NewStatus?.ToString(),
            PreviousExpiryDate = h.PreviousExpiryDate,
            NewExpiryDate = h.NewExpiryDate,
            PerformedBy = h.CreatedBy,
            IpAddress = h.IpAddress
        }).ToList();
    }

    /// <summary>
    /// Helper method to record subscription history
    /// </summary>
    private async Task RecordHistoryAsync(Guid subscriptionId, string action, LicenseStatus? previousStatus = null, 
        LicenseStatus? newStatus = null, DateTime? previousExpiryDate = null, DateTime? newExpiryDate = null, 
        string? reason = null, string? notes = null, string? ipAddress = null)
    {
        var historyEntry = new SubscriptionHistory
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Action = action,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PreviousExpiryDate = previousExpiryDate,
            NewExpiryDate = newExpiryDate,
            Reason = reason,
            Notes = notes,
            PerformedByAdminId = _currentUserService.UserId,
            IpAddress = ipAddress,
            UserAgent = null // Could be passed from controller if needed
        };

        await _historyRepo.AddHistoryEntryAsync(historyEntry);
    }

    public async Task<SubscriptionAnalyticsDto> GetSubscriptionAnalyticsAsync(Guid subscriptionId, DateTime? fromDate, DateTime? toDate)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        var from = fromDate ?? DateTime.UtcNow.AddMonths(-6);
        var to = toDate ?? DateTime.UtcNow;
        var now = DateTime.UtcNow;

        // Get history for this subscription
        var history = await _historyRepo.GetBySubscriptionIdAsync(subscriptionId);
        var recentHistory = history
            .OrderByDescending(h => h.CreatedTimestamp)
            .Take(10)
            .Select(h => new SubscriptionHistoryItemDto
            {
                Action = h.Action,
                Reason = h.Reason,
                Timestamp = h.CreatedTimestamp,
                PerformedBy = h.PerformedByAdminId?.ToString()
            })
            .ToList();

        // Calculate days remaining
        var daysRemaining = subscription.IsLifetime ? 999999 : 
            subscription.IsExpired ? 0 : 
            Math.Max(0, (subscription.ExpiryDateUtc - now).Days);

        // Calculate utilization
        var totalDays = Math.Max(1, (subscription.ExpiryDateUtc - subscription.StartDateUtc).Days);
        var activeDays = subscription.IsActive ? Math.Max(0, (now - subscription.StartDateUtc).Days) : 0;
        var utilizationPercentage = Math.Min(100, Math.Round((activeDays / (double)totalDays) * 100, 2));

        // Generate monthly trends (last 6 months)
        var monthlyTrends = GenerateMonthlyTrends(subscription, from, to);

        // Get status distribution for company's subscriptions
        var statusDistribution = await GetStatusDistributionAsync(subscription.CompanyId);

        // Get feature usage based on plan features
        var featureUsage = GetFeatureUsage(subscription);

        // Determine status label
        var statusLabel = subscription.IsExpired ? "Expired" :
            !subscription.IsActive ? "Suspended" :
            subscription.IsTrial ? "Trial" :
            subscription.IsLifetime ? "Lifetime" : "Active";

        return new SubscriptionAnalyticsDto
        {
            SubscriptionId = subscriptionId,
            CompanyName = subscription.Company.Name,
            PlanName = subscription.Plan.Name,
            Period = new AnalyticsPeriodDto { From = from, To = to },
            Status = new SubscriptionStatusAnalyticsDto
            {
                IsActive = subscription.IsActive,
                IsExpired = subscription.IsExpired,
                IsTrial = subscription.IsTrial,
                IsLifetime = subscription.IsLifetime,
                DaysRemaining = daysRemaining,
                StatusLabel = statusLabel
            },
            Usage = new UsageAnalyticsDto
            {
                TotalDays = totalDays,
                ActiveDays = activeDays,
                UtilizationPercentage = utilizationPercentage,
                TotalLogins = 0, // Would come from actual login tracking
                UniqueUsers = 0, // Would come from actual user tracking
                ApiCalls = 0 // Would come from API tracking
            },
            MonthlyTrends = monthlyTrends,
            StatusDistribution = statusDistribution,
            FeatureUsage = featureUsage,
            Performance = new PerformanceMetricsDto(), // Default values for now
            RecentHistory = recentHistory
        };
    }

    private List<MonthlyUsageDto> GenerateMonthlyTrends(Subscription subscription, DateTime from, DateTime to)
    {
        var trends = new List<MonthlyUsageDto>();
        var current = new DateTime(from.Year, from.Month, 1);
        var end = new DateTime(to.Year, to.Month, 1);

        while (current <= end)
        {
            var monthStart = current;
            var monthEnd = current.AddMonths(1).AddDays(-1);
            
            // Calculate if subscription was active during this month
            var wasActive = subscription.StartDateUtc <= monthEnd && 
                           (subscription.IsLifetime || subscription.ExpiryDateUtc >= monthStart) &&
                           subscription.IsActive;

            var daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);
            var activeDaysInMonth = 0;

            if (wasActive)
            {
                var effectiveStart = subscription.StartDateUtc > monthStart ? subscription.StartDateUtc : monthStart;
                var effectiveEnd = subscription.IsLifetime ? monthEnd : 
                    (subscription.ExpiryDateUtc < monthEnd ? subscription.ExpiryDateUtc : monthEnd);
                activeDaysInMonth = Math.Max(0, (effectiveEnd - effectiveStart).Days + 1);
            }

            var usagePercentage = Math.Round((activeDaysInMonth / (double)daysInMonth) * 100, 1);

            trends.Add(new MonthlyUsageDto
            {
                Month = current.ToString("MMM"),
                Year = current.Year,
                Usage = usagePercentage,
                Revenue = subscription.Amount,
                ActiveDays = activeDaysInMonth
            });

            current = current.AddMonths(1);
        }

        return trends;
    }

    private async Task<List<StatusDistributionDto>> GetStatusDistributionAsync(Guid companyId)
    {
        var allSubscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(companyId);
        
        var active = allSubscriptions.Count(s => s.IsActive && !s.IsExpired && !s.IsTrial);
        var trial = allSubscriptions.Count(s => s.IsTrial && s.IsActive);
        var suspended = allSubscriptions.Count(s => !s.IsActive && !s.IsExpired);
        var expired = allSubscriptions.Count(s => s.IsExpired);

        var total = Math.Max(1, active + trial + suspended + expired);

        return new List<StatusDistributionDto>
        {
            new() { Name = "Active", Value = (int)Math.Round((active / (double)total) * 100), Color = "#10b981" },
            new() { Name = "Trial", Value = (int)Math.Round((trial / (double)total) * 100), Color = "#3b82f6" },
            new() { Name = "Suspended", Value = (int)Math.Round((suspended / (double)total) * 100), Color = "#f59e0b" },
            new() { Name = "Expired", Value = (int)Math.Round((expired / (double)total) * 100), Color = "#ef4444" }
        };
    }

    private List<FeatureUsageDto> GetFeatureUsage(Subscription subscription)
    {
        var features = new List<FeatureUsageDto>();

        // Add plan projects as features
        foreach (var project in subscription.Plan.PlanProjects)
        {
            features.Add(new FeatureUsageDto
            {
                Feature = project.Project.Name,
                Usage = subscription.IsActive ? 100 : 0, // Would come from actual usage tracking
                IsEnabled = true
            });
        }

        // Add plan modules as features
        foreach (var module in subscription.Plan.PlanModules)
        {
            features.Add(new FeatureUsageDto
            {
                Feature = module.Module.Name,
                Usage = subscription.IsActive ? 100 : 0, // Would come from actual usage tracking
                IsEnabled = true
            });
        }

        // Add custom features
        foreach (var feature in subscription.Plan.CustomFeatures)
        {
            features.Add(new FeatureUsageDto
            {
                Feature = feature,
                Usage = subscription.IsActive ? 100 : 0,
                IsEnabled = true
            });
        }

        return features;
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

    /// <summary>
    /// Converts subscription amount to the display currency
    /// </summary>
    private async Task<SubscriptionDto> ConvertSubscriptionCurrencyAsync(SubscriptionDto dto, string displayCurrency)
    {
        try
        {
            // Parse the display currency string to Currency enum
            if (!Enum.TryParse<Currency>(displayCurrency, true, out var targetCurrency))
            {
                // Try parsing as integer (enum value)
                if (int.TryParse(displayCurrency, out var currencyValue) && Enum.IsDefined(typeof(Currency), currencyValue))
                {
                    targetCurrency = (Currency)currencyValue;
                }
                else
                {
                    _logger.LogWarning("Invalid display currency: {Currency}", displayCurrency);
                    return dto;
                }
            }

            // Get source currency from subscription
            var sourceCurrency = dto.Currency;
            
            // Skip if already in target currency
            if (sourceCurrency == targetCurrency)
            {
                return dto;
            }

            // Convert amount
            var convertedAmount = await _currencyExchangeService.ConvertAsync(dto.Amount, sourceCurrency, targetCurrency);
            
            // Update DTO with converted values
            dto.Amount = convertedAmount;
            dto.Currency = targetCurrency;
            
            // Also convert PlanAmount if it exists
            if (dto.PlanAmount > 0)
            {
                dto.PlanAmount = await _currencyExchangeService.ConvertAsync(dto.PlanAmount, dto.PlanCurrency, targetCurrency);
                dto.PlanCurrency = targetCurrency;
            }

            _logger.LogDebug("Converted subscription {Id} amount from {SourceCurrency} {OriginalAmount} to {TargetCurrency} {ConvertedAmount}",
                dto.Id, sourceCurrency, dto.Amount, targetCurrency, convertedAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert subscription currency to {DisplayCurrency}", displayCurrency);
            // Return original DTO on error
        }

        return dto;
    }

    #endregion
}

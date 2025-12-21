using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.OnlineAccess;
using Application.Services;
using AutoMapper;
using Domain.Entities.OnlineAccess;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Infrastructure.Resources;
using Infrastructure.Settings;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class OnlineClientService : IOnlineClientService
{
    private readonly IOnlineClientTokenRepository _tokenRepository;
    private readonly IOnlineDeviceBindingRepository _deviceRepository;
    private readonly ISubscriptionChangeLogRepository _changeLogRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanEntitlementRepository _entitlementRepository;
    private readonly IOnlineJwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OnlineTokenSettings _settings;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public OnlineClientService(
        IOnlineClientTokenRepository tokenRepository,
        IOnlineDeviceBindingRepository deviceRepository,
        ISubscriptionChangeLogRepository changeLogRepository,
        ISubscriptionRepository subscriptionRepository,
        IPlanEntitlementRepository entitlementRepository,
        IOnlineJwtService jwtService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IOptions<OnlineTokenSettings> settings,
        IStringLocalizer<SharedResource> localizer)
    {
        _tokenRepository = tokenRepository;
        _deviceRepository = deviceRepository;
        _changeLogRepository = changeLogRepository;
        _subscriptionRepository = subscriptionRepository;
        _entitlementRepository = entitlementRepository;
        _jwtService = jwtService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _localizer = localizer;
    }

    #region Token Management

    public async Task<GenerateOnlineTokenResponse> GenerateTokenAsync(GenerateOnlineTokenRequest request, CancellationToken cancellationToken = default)
    {
        // Validate subscription exists and is active
        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, null, cancellationToken);
        if (subscription == null)
        {
            return new GenerateOnlineTokenResponse
            {
                Success = false,
                Message = _localizer["Subscription.NotFound"]
            };
        }

        if (!subscription.IsActive && !subscription.IsTrial)
        {
            return new GenerateOnlineTokenResponse
            {
                Success = false,
                Message = _localizer["Subscription.NotActive"]
            };
        }

        if (subscription.IsOffline)
        {
            return new GenerateOnlineTokenResponse
            {
                Success = false,
                Message = _localizer["Subscription.OnlineOnly"]
            };
        }

        // Calculate expiry
        var expiryDays = request.ExpiryDays ?? _settings.DefaultExpiryDays;
        if (expiryDays > _settings.MaxExpiryDays)
            expiryDays = _settings.MaxExpiryDays;

        // Don't exceed subscription expiry
        var maxExpiry = subscription.ExpiryDateUtc;
        var calculatedExpiry = DateTime.UtcNow.AddDays(expiryDays);
        var actualExpiry = calculatedExpiry < maxExpiry ? calculatedExpiry : maxExpiry;

        // Create token entity
        var tokenEntity = new OnlineClientToken
        {
            Id = Guid.NewGuid(),
            CompanyId = subscription.CompanyId,
            SubscriptionId = subscription.Id,
            Name = request.Name,
            TokenHash = "", // Will be set after JWT generation
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = actualExpiry,
            Status = ClientTokenStatus.Active,
            AutoRefreshEnabled = request.AutoRefreshEnabled,
            MaxDevices = request.MaxDevices ?? _settings.DefaultMaxDevices,
            Notes = request.Notes
        };

        // Generate JWT
        var jwtToken = _jwtService.GenerateToken(tokenEntity);
        tokenEntity.TokenHash = _jwtService.ComputeTokenHash(jwtToken);

        await _tokenRepository.AddAsync(tokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with includes
        var savedToken = await _tokenRepository.GetWithDevicesAsync(tokenEntity.Id, cancellationToken);

        return new GenerateOnlineTokenResponse
        {
            Success = true,
            Message = _localizer["OnlineToken.Generated"],
            Token = jwtToken, // Only returned once!
            TokenInfo = _mapper.Map<OnlineClientTokenDto>(savedToken)
        };
    }

    public async Task<OnlineTokenValidationDto> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Validate JWT structure and signature
        var claims = _jwtService.ValidateToken(token);
        if (claims == null)
        {
            return new OnlineTokenValidationDto
            {
                IsValid = false,
                Message = _localizer["OnlineToken.Invalid"]
            };
        }

        // Verify token exists in database and is active
        var tokenHash = _jwtService.ComputeTokenHash(token);
        var tokenEntity = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (tokenEntity == null)
        {
            return new OnlineTokenValidationDto
            {
                IsValid = false,
                Message = _localizer["OnlineToken.NotFound"]
            };
        }

        if (tokenEntity.Status == ClientTokenStatus.Revoked)
        {
            return new OnlineTokenValidationDto
            {
                IsValid = false,
                Message = _localizer["OnlineToken.Revoked"]
            };
        }

        if (tokenEntity.IsExpired)
        {
            return new OnlineTokenValidationDto
            {
                IsValid = false,
                Message = _localizer["OnlineToken.Expired"],
                SubscriptionStatus = GetSubscriptionStatus(tokenEntity.Subscription),
                IsSubscriptionUsable = IsSubscriptionUsable(tokenEntity.Subscription)
            };
        }

        // Compute subscription status for all responses
        var subscriptionStatus = GetSubscriptionStatus(tokenEntity.Subscription);
        var isSubscriptionUsable = IsSubscriptionUsable(tokenEntity.Subscription);

        // Verify subscription is still active
        if (!tokenEntity.Subscription.IsActive && !tokenEntity.Subscription.IsTrial && !tokenEntity.Subscription.IsPaused)
        {
            return new OnlineTokenValidationDto
            {
                IsValid = false,
                Message = _localizer["Subscription.NotActive"],
                SubscriptionStatus = subscriptionStatus,
                IsSubscriptionUsable = isSubscriptionUsable
            };
        }

        if (tokenEntity.Subscription.IsOffline)
        {
            return new OnlineTokenValidationDto
            {
                IsValid = false,
                Message = _localizer["Subscription.OnlineOnly"],
                SubscriptionStatus = subscriptionStatus,
                IsSubscriptionUsable = isSubscriptionUsable
            };
        }

        var validationResult = new OnlineTokenValidationDto
        {
            IsValid = true,
            Message = _localizer["OnlineToken.Valid"],
            TokenId = tokenEntity.Id,
            CompanyId = tokenEntity.CompanyId,
            SubscriptionId = tokenEntity.SubscriptionId,
            CompanyName = tokenEntity.Company?.Name,
            PlanName = tokenEntity.Subscription?.Plan?.Name,
            ExpiresAtUtc = tokenEntity.ExpiresAtUtc,
            SubscriptionStatus = subscriptionStatus,
            IsSubscriptionUsable = isSubscriptionUsable
        };
        
        // Encrypt IDs in response
        return _mapper.Map<OnlineTokenValidationDto, OnlineTokenValidationDto>(validationResult);
    }

    /// <summary>
    /// Returns a human-readable subscription status string.
    /// </summary>
    private static string GetSubscriptionStatus(Domain.Entities.Subscription subscription)
    {
        if (subscription.IsExpired) return "Expired";
        if (!subscription.IsActive && subscription.IsTrial) return "Trial";
        if (subscription.IsPaused) return "Paused";
        if (!subscription.IsActive) return "Suspended";
        return "Active";
    }

    /// <summary>
    /// Returns true if the subscription allows token/license usage.
    /// </summary>
    private static bool IsSubscriptionUsable(Domain.Entities.Subscription subscription)
    {
        return subscription.IsActive || subscription.IsTrial || subscription.IsPaused;
    }

    public async Task<IEnumerable<OnlineClientTokenDto>> GetTokensByCompanyAsync(Guid companyId, bool includeRevoked = false, CancellationToken cancellationToken = default)
    {
        var tokens = await _tokenRepository.GetByCompanyIdAsync(companyId, includeRevoked, cancellationToken);
        return _mapper.Map<IEnumerable<OnlineClientTokenDto>>(tokens);
    }

    public async Task<IEnumerable<OnlineClientTokenDto>> GetTokensBySubscriptionAsync(Guid subscriptionId, bool includeRevoked = false, CancellationToken cancellationToken = default)
    {
        var tokens = await _tokenRepository.GetBySubscriptionIdAsync(subscriptionId, includeRevoked, cancellationToken);
        return _mapper.Map<IEnumerable<OnlineClientTokenDto>>(tokens);
    }

    public async Task<bool> RevokeTokenAsync(Guid tokenId, string reason, CancellationToken cancellationToken = default)
    {
        var token = await _tokenRepository.GetByIdAsync(tokenId, null, cancellationToken);
        if (token == null) return false;

        token.Status = ClientTokenStatus.Revoked;
        token.RevocationReason = reason;
        token.UpdatedTimestamp = DateTime.UtcNow;

        await _tokenRepository.UpdateAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task UpdateTokenUsageAsync(Guid tokenId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        await _tokenRepository.UpdateLastUsedAsync(tokenId, ipAddress, userAgent, cancellationToken);
    }

    public async Task<GenerateOnlineTokenResponse> RegenerateTokenAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        var oldToken = await _tokenRepository.GetByIdAsync(tokenId, null, cancellationToken);
        if (oldToken == null)
        {
            return new GenerateOnlineTokenResponse
            {
                Success = false,
                Message = _localizer["OnlineToken.NotFound"]
            };
        }

        // Revoke old token
        oldToken.Status = ClientTokenStatus.Revoked;
        oldToken.RevocationReason = "Regenerated";
        oldToken.UpdatedTimestamp = DateTime.UtcNow;
        await _tokenRepository.UpdateAsync(oldToken, cancellationToken);

        // Create new token with same settings
        var request = new GenerateOnlineTokenRequest
        {
            SubscriptionId = oldToken.SubscriptionId,
            Name = oldToken.Name,
            AutoRefreshEnabled = oldToken.AutoRefreshEnabled,
            MaxDevices = oldToken.MaxDevices,
            Notes = oldToken.Notes
        };

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GenerateTokenAsync(request, cancellationToken);
    }

    #endregion

    #region Device Management

    public async Task<RegisterDeviceResponse> RegisterDeviceAsync(Guid subscriptionId, RegisterDeviceRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        // Check if already registered
        var existing = await _deviceRepository.GetByFingerprintAsync(subscriptionId, request.DeviceFingerprint, cancellationToken);
        if (existing != null && existing.Status == OnlineDeviceStatus.Active)
        {
            // Update last seen and return success
            await _deviceRepository.UpdateLastSeenAsync(existing.Id, ipAddress, userAgent, cancellationToken);
            
            var count = await _deviceRepository.GetActiveDeviceCountAsync(subscriptionId, cancellationToken);
            
            var alreadyRegisteredResponse = new RegisterDeviceResponse
            {
                Success = true,
                Message = _localizer["OnlineDevice.AlreadyRegistered"],
                DeviceId = existing.Id,
                CurrentDeviceCount = count
            };
            
            // Encrypt IDs in response
            return _mapper.Map<RegisterDeviceResponse, RegisterDeviceResponse>(alreadyRegisteredResponse);
        }

        // Check device limit
        var subscription = await _subscriptionRepository.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            return new RegisterDeviceResponse
            {
                Success = false,
                Message = _localizer["Subscription.NotFound"]
            };
        }

        if (subscription.IsOffline)
        {
            return new RegisterDeviceResponse
            {
                Success = false,
                Message = _localizer["Subscription.OnlineOnly"]
            };
        }

        // Get active token for this subscription (needed for device limit check)
        var token = await _tokenRepository.GetActiveTokenForSubscriptionAsync(subscriptionId, cancellationToken);
        if (token == null)
        {
            return new RegisterDeviceResponse
            {
                Success = false,
                Message = _localizer["OnlineToken.NoActiveToken"]
            };
        }

        var currentCount = await _deviceRepository.GetActiveDeviceCountAsync(subscriptionId, cancellationToken);
        
        // Use effective device limit: token.MaxDevices (if set) OR subscription override OR plan default
        // Priority: Token > Subscription Override > Plan
        var subscriptionMaxDevices = subscription.EffectiveMaxDevices;
        var tokenMaxDevices = token.MaxDevices;
        
        // Take the most restrictive limit (smallest non-zero value)
        int maxDevices;
        if (tokenMaxDevices.HasValue && tokenMaxDevices.Value > 0)
        {
            // Token has a specific limit - use it (can be more or less restrictive than subscription)
            maxDevices = tokenMaxDevices.Value;
        }
        else
        {
            // Use subscription's effective limit
            maxDevices = subscriptionMaxDevices;
        }

        // Check admission mode - if AdminOnly, devices cannot self-register
        var admissionMode = subscription.EffectiveDeviceAdmissionMode;
        if (admissionMode == DeviceAdmissionMode.AdminOnly)
        {
            return new RegisterDeviceResponse
            {
                Success = false,
                Message = _localizer["OnlineDevice.AdminApprovalRequired"],
                RequiresAdminApproval = true,
                CurrentDeviceCount = currentCount,
                MaxDevicesAllowed = maxDevices > 0 ? maxDevices : null
            };
        }

        // Check HybridAutoAdmin mode - if beyond auto-admit threshold, require admin approval
        if (admissionMode == DeviceAdmissionMode.HybridAutoAdmin)
        {
            var maxAutoAdmit = subscription.EffectiveMaxAutoAdmitDevices;
            if (currentCount >= maxAutoAdmit)
            {
                return new RegisterDeviceResponse
                {
                    Success = false,
                    Message = _localizer["OnlineDevice.AdminApprovalRequired"],
                    RequiresAdminApproval = true,
                    CurrentDeviceCount = currentCount,
                    MaxDevicesAllowed = maxDevices > 0 ? maxDevices : null
                };
            }
        }

        // Check device limit
        if (maxDevices > 0 && currentCount >= maxDevices)
        {
            // AutoWithQueue mode - queue for admin approval instead of blocking
            if (admissionMode == DeviceAdmissionMode.AutoWithQueue)
            {
                return new RegisterDeviceResponse
                {
                    Success = false,
                    Message = _localizer["OnlineDevice.QueuedForApproval"],
                    RequiresAdminApproval = true,
                    LimitReached = true,
                    CurrentDeviceCount = currentCount,
                    MaxDevicesAllowed = maxDevices
                };
            }
            
            return new RegisterDeviceResponse
            {
                Success = false,
                Message = _localizer["OnlineDevice.LimitReached"],
                LimitReached = true,
                CurrentDeviceCount = currentCount,
                MaxDevicesAllowed = maxDevices
            };
        }

        // Create device binding
        var device = new OnlineDeviceBinding
        {
            Id = Guid.NewGuid(),
            TokenId = token.Id,
            SubscriptionId = subscriptionId,
            DeviceFingerprint = request.DeviceFingerprint,
            DeviceName = request.DeviceName,
            DeviceType = request.DeviceType,
            OperatingSystem = request.OperatingSystem,
            Status = OnlineDeviceStatus.Active,
            FirstSeenAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow,
            LastIpAddress = ipAddress,
            LastUserAgent = userAgent
        };

        await _deviceRepository.AddAsync(device, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RegisterDeviceResponse
        {
            Success = true,
            Message = _localizer["OnlineDevice.Registered"],
            DeviceId = device.Id,
            CurrentDeviceCount = currentCount + 1,
            MaxDevicesAllowed = maxDevices > 0 ? maxDevices : null
        };
        
        // Encrypt IDs in response
        return _mapper.Map<RegisterDeviceResponse, RegisterDeviceResponse>(response);
    }

    public async Task<bool> UnregisterDeviceAsync(Guid subscriptionId, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByFingerprintAsync(subscriptionId, deviceFingerprint, cancellationToken);
        if (device == null) return false;

        device.Status = OnlineDeviceStatus.Revoked;
        device.StatusReason = "Unregistered";
        device.UpdatedTimestamp = DateTime.UtcNow;

        await _deviceRepository.UpdateAsync(device, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> UnregisterDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId, null, cancellationToken);
        if (device == null) return false;

        device.Status = OnlineDeviceStatus.Revoked;
        device.StatusReason = "Unregistered by admin";
        device.UpdatedTimestamp = DateTime.UtcNow;

        await _deviceRepository.UpdateAsync(device, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<OnlineDeviceDto>> GetDevicesAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var devices = await _deviceRepository.GetBySubscriptionIdAsync(subscriptionId, false, cancellationToken);
        return _mapper.Map<IEnumerable<OnlineDeviceDto>>(devices);
    }

    public async Task<DeviceLimitDto> GetDeviceLimitAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithDetailsAsync(subscriptionId, cancellationToken);
        var currentCount = await _deviceRepository.GetActiveDeviceCountAsync(subscriptionId, cancellationToken);
        
        // Use effective device limit from subscription (override > plan)
        var maxDevices = subscription?.EffectiveMaxDevices ?? _settings.DefaultMaxDevices;
        var admissionMode = subscription?.EffectiveDeviceAdmissionMode ?? DeviceAdmissionMode.Open;

        return new DeviceLimitDto
        {
            CurrentCount = currentCount,
            MaxAllowed = maxDevices > 0 ? maxDevices : null,
            IsUnlimited = maxDevices == 0,
            RequiresAdminApproval = admissionMode == DeviceAdmissionMode.AdminOnly,
            AdmissionMode = admissionMode.ToString()
        };
    }

    public async Task UpdateDeviceActivityAsync(Guid subscriptionId, string deviceFingerprint, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByFingerprintAsync(subscriptionId, deviceFingerprint, cancellationToken);
        if (device != null)
        {
            await _deviceRepository.IncrementApiCallCountAsync(device.Id, cancellationToken);
        }
    }

    #endregion

    #region Entitlements

    public async Task<EntitlementMatrixDto> GetEntitlementsAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            return new EntitlementMatrixDto
            {
                Version = 0,
                AccessMode = SubscriptionAccessMode.Blocked
            };
        }

        var entitlements = await _entitlementRepository.GetByPlanIdAsync(subscription.PlanId, cancellationToken);

        var dto = new EntitlementMatrixDto
        {
            Version = subscription.EntitlementsVersion,
            AccessMode = subscription.AccessMode,
            DaysRemaining = Math.Max(0, (int)(subscription.ExpiryDateUtc - DateTime.UtcNow).TotalDays),
            IsInGracePeriod = subscription.AccessMode == SubscriptionAccessMode.GracePeriod,
            ExpiresAtUtc = subscription.ExpiryDateUtc,
            LastUpdatedUtc = subscription.UpdatedTimestamp ?? subscription.CreatedTimestamp
        };

        // Map project entitlements
        foreach (var ent in entitlements.Where(e => e.ProjectId != null))
        {
            dto.Projects.Add(new ProjectAccessDto
            {
                ProjectId = ent.ProjectId!.Value,
                ProjectName = ent.Project?.Name ?? "",
                ProjectCode = ent.Project?.Name ?? "",
                AccessLevel = ent.AccessLevel,
                CanCreate = ent.CanCreate,
                CanRead = ent.CanRead,
                CanUpdate = ent.CanUpdate,
                CanDelete = ent.CanDelete,
                CanExport = ent.CanExport
            });
        }

        // Map module entitlements
        foreach (var ent in entitlements.Where(e => e.ModuleId != null))
        {
            dto.Modules.Add(new ModuleAccessDto
            {
                ModuleId = ent.ModuleId!.Value,
                ProjectId = ent.ProjectId,
                ModuleName = ent.Module?.Name ?? "",
                ModuleCode = ent.Module?.Name ?? "",
                AccessLevel = ent.AccessLevel,
                CanCreate = ent.CanCreate,
                CanRead = ent.CanRead,
                CanUpdate = ent.CanUpdate,
                CanDelete = ent.CanDelete,
                CanExport = ent.CanExport,
                Features = ent.Module?.Features?.ToList() ?? new List<string>()
            });
        }

        // Encrypt IDs in response
        return _mapper.Map<EntitlementMatrixDto, EntitlementMatrixDto>(dto);
    }

    public async Task<int> GetEntitlementVersionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, null, cancellationToken);
        return subscription?.EntitlementsVersion ?? 0;
    }

    public async Task<OnlineSubscriptionStatusDto> GetSubscriptionStatusAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            var notFoundDto = new OnlineSubscriptionStatusDto
            {
                SubscriptionId = subscriptionId,
                Status = "NotFound",
                CompanyName = "",
                PlanName = ""
            };
            // Encrypt IDs in response even for not found
            return _mapper.Map<OnlineSubscriptionStatusDto, OnlineSubscriptionStatusDto>(notFoundDto);
        }

        var deviceCount = await _deviceRepository.GetActiveDeviceCountAsync(subscriptionId, cancellationToken);
        var pendingChanges = await _changeLogRepository.GetPendingChangesAsync(subscriptionId, cancellationToken);

        var statusDto = new OnlineSubscriptionStatusDto
        {
            SubscriptionId = subscription.Id,
            CompanyId = subscription.CompanyId,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name ?? "",
            CompanyName = subscription.Company?.Name ?? "",
            Status = subscription.IsActive ? "Active" : subscription.IsTrial ? "Trial" : subscription.IsExpired ? "Expired" : "Inactive",
            AccessMode = subscription.AccessMode,
            StartDateUtc = subscription.StartDateUtc,
            ExpiresAtUtc = subscription.ExpiryDateUtc,
            DaysRemaining = Math.Max(0, (int)(subscription.ExpiryDateUtc - DateTime.UtcNow).TotalDays),
            IsActive = subscription.IsActive,
            IsTrial = subscription.IsTrial,
            IsInGracePeriod = subscription.AccessMode == SubscriptionAccessMode.GracePeriod,
            EntitlementVersion = subscription.EntitlementsVersion,
            MaxDevices = subscription.Plan?.MaxDevices,
            CurrentDeviceCount = deviceCount,
            HasPendingChanges = pendingChanges.Any(),
            PendingChangeCount = pendingChanges.Count()
        };
        
        // Encrypt IDs in response
        return _mapper.Map<OnlineSubscriptionStatusDto, OnlineSubscriptionStatusDto>(statusDto);
    }

    #endregion

    #region Pending Changes

    public async Task<PendingChangesDto> GetPendingChangesAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var changes = await _changeLogRepository.GetPendingChangesAsync(subscriptionId, cancellationToken);
        var changesList = changes.ToList();

        var pendingDto = new PendingChangesDto
        {
            SubscriptionId = subscriptionId,
            TotalPendingChanges = changesList.Count,
            NextChangeDate = changesList.MinBy(c => c.EffectiveDateUtc)?.EffectiveDateUtc,
            Changes = _mapper.Map<List<SubscriptionChangeLogDto>>(changesList)
        };
        
        // Encrypt IDs in response
        return _mapper.Map<PendingChangesDto, PendingChangesDto>(pendingDto);
    }

    #endregion
}

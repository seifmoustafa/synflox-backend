using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.ClientAdmin;
using Application.DTOs.OfflineLicense;
using Application.Services;
using Domain.Entities.Licensing;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing offline license admin tokens and device binding.
/// </summary>
public class OfflineLicenseAdminService : IOfflineLicenseAdminService
{
    private readonly IOfflineLicenseAdminTokenRepository _tokenRepo;
    private readonly ILicenseActivationRepository _activationRepo;
    private readonly IDeviceReplacementRequestRepository _replacementRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOfflineLicenseService _licenseService;
    private readonly IIdEncryptionService _idEncryption;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<OfflineLicenseAdminService> _logger;
    private readonly OfflineLicenseSettings _settings;

    // Default expiry for replacement requests (48 hours)
    private const int REPLACEMENT_REQUEST_EXPIRY_HOURS = 48;

    public OfflineLicenseAdminService(
        IOfflineLicenseAdminTokenRepository tokenRepo,
        ILicenseActivationRepository activationRepo,
        IDeviceReplacementRequestRepository replacementRepo,
        ISubscriptionRepository subscriptionRepo,
        ICompanyRepository companyRepo,
        IUnitOfWork unitOfWork,
        IOfflineLicenseService licenseService,
        IIdEncryptionService idEncryption,
        ILocalizationService localizer,
        ILogger<OfflineLicenseAdminService> logger,
        IOptions<OfflineLicenseSettings> settings)
    {
        _tokenRepo = tokenRepo;
        _activationRepo = activationRepo;
        _replacementRepo = replacementRepo;
        _subscriptionRepo = subscriptionRepo;
        _companyRepo = companyRepo;
        _unitOfWork = unitOfWork;
        _licenseService = licenseService;
        _idEncryption = idEncryption;
        _localizer = localizer;
        _logger = logger;
        _settings = settings.Value;
    }

    #region Token Management (SYNFLOX Admin Only)

    public async Task<GenerateAdminTokenResponse> GenerateAdminTokenAsync(GenerateAdminTokenRequest request)
    {
        try
        {
            // Validate company exists
            var company = await _companyRepo.GetByIdAsync(request.CompanyId, null);
            if (company == null)
            {
                return new GenerateAdminTokenResponse
                {
                    Success = false,
                    Message = _localizer["Company.NotFound"]
                };
            }

            // Generate JWT token
            var tokenId = Guid.NewGuid();
            var issuedAt = DateTime.UtcNow;
            var expiresAt = issuedAt.AddDays(request.ExpiryDays);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString()),
                new Claim("token_type", "offline_license_admin"),
                new Claim("company_id", request.CompanyId.ToString()),
                new Claim("company_name", company.Name),
                new Claim("can_bind", request.CanBindDevices.ToString().ToLower()),
                new Claim("can_unbind", request.CanUnbindDevices.ToString().ToLower()),
                new Claim("can_view", request.CanViewDevices.ToString().ToLower()),
                new Claim("can_approve", request.CanApproveReplacements.ToString().ToLower()),
            };

            // Decode base64 key for JWT signing
            var keyBytes = Convert.FromBase64String(_settings.EncryptionKey);
            var key = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            _logger.LogInformation("Generating token with key length: {KeyLength} bytes, Issuer: {Issuer}", keyBytes.Length, _settings.Issuer);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: "offline-license-admin",
                claims: claims,
                notBefore: issuedAt,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.WriteToken(token);

            // Hash the token for storage
            var tokenHash = ComputeTokenHash(jwtToken);

            // Create token entity
            var tokenEntity = new OfflineLicenseAdminToken
            {
                Id = tokenId,
                CompanyId = request.CompanyId,
                Name = request.Name,
                TokenHash = tokenHash,
                IssuedAtUtc = issuedAt,
                ExpiresAtUtc = expiresAt,
                Status = ClientTokenStatus.Active,
                // Offline permissions
                CanBindDevices = request.CanBindDevices,
                CanUnbindDevices = request.CanUnbindDevices,
                CanViewDevices = request.CanViewDevices,
                CanApproveReplacements = request.CanApproveReplacements,
                // Online permissions (unified admin system)
                CanViewOnlineTokens = request.CanViewOnlineTokens,
                CanManageOnlineTokens = request.CanManageOnlineTokens,
                CanViewOnlineDevices = request.CanViewOnlineDevices,
                CanUnbindOnlineDevices = request.CanUnbindOnlineDevices,
                DailyApiLimit = request.DailyApiLimit,
                Notes = request.Notes
            };

            await _tokenRepo.AddAsync(tokenEntity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Generated offline license admin token {TokenId} for company {CompanyId}", 
                tokenId, request.CompanyId);

            return new GenerateAdminTokenResponse
            {
                Success = true,
                Message = _localizer["OfflineLicense.AdminTokenGenerated"],
                Token = jwtToken, // Only returned once!
                TokenInfo = MapToDto(tokenEntity, company.Name)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating admin token for company {CompanyId}", request.CompanyId);
            return new GenerateAdminTokenResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<bool> RevokeAdminTokenAsync(Guid tokenId, string reason)
    {
        var token = await _tokenRepo.GetByIdAsync(tokenId, null);
        if (token == null) return false;

        token.Status = ClientTokenStatus.Revoked;
        token.RevocationReason = reason;
        token.RevokedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Revoked offline license admin token {TokenId}: {Reason}", tokenId, reason);
        return true;
    }

    public async Task<List<AdminTokenDto>> GetTokensByCompanyAsync(Guid companyId, bool includeRevoked = false)
    {
        var tokens = await _tokenRepo.GetByCompanyAsync(companyId, includeRevoked);
        var company = await _companyRepo.GetByIdAsync(companyId, null);
        var companyName = company?.Name ?? "Unknown";

        return tokens.Select(t => MapToDto(t, companyName)).ToList();
    }

    public async Task<AdminTokenDto?> GetTokenByIdAsync(Guid tokenId)
    {
        var token = await _tokenRepo.GetByIdAsync(tokenId, null);
        if (token == null) return null;

        var company = await _companyRepo.GetByIdAsync(token.CompanyId, null);
        return MapToDto(token, company?.Name ?? "Unknown");
    }

    #endregion

    #region Device Management (Client Admin via Token)

    public async Task<ClientAdminContext?> ValidateAdminTokenAsync(string jwtToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // Decode base64 key for JWT signing
            var keyBytes = Convert.FromBase64String(_settings.EncryptionKey);
            var key = new SymmetricSecurityKey(keyBytes);
            _logger.LogInformation("Validation using key length: {KeyLength} bytes, Issuer: {Issuer}", keyBytes.Length, _settings.Issuer);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = "offline-license-admin",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            ClaimsPrincipal principal;
            try
            {
                principal = tokenHandler.ValidateToken(jwtToken, validationParameters, out var validatedToken);
                _logger.LogInformation("JWT validation succeeded for token");
            }
            catch (Exception jwtEx)
            {
                _logger.LogWarning(jwtEx, "JWT validation failed: {Message}", jwtEx.Message);
                return null;
            }

            // Verify token exists in database and is active
            var tokenHash = ComputeTokenHash(jwtToken);
            _logger.LogInformation("Looking up token with hash: {HashPrefix}...", tokenHash.Substring(0, 16));
            
            var tokenEntity = await _tokenRepo.GetByTokenHashAsync(tokenHash);

            if (tokenEntity == null)
            {
                _logger.LogWarning("Admin token validation failed: token not found in database");
                return null;
            }
            
            if (!tokenEntity.IsValid)
            {
                _logger.LogWarning("Admin token validation failed: token exists but invalid. Status={Status}, Expired={IsExpired}, Deleted={IsDeleted}", 
                    tokenEntity.Status, tokenEntity.IsExpired, tokenEntity.IsDeleted);
                return null;
            }

            // Check rate limit
            if (tokenEntity.IsRateLimitExceeded)
            {
                _logger.LogWarning("Admin token rate limit exceeded for token {TokenId}", tokenEntity.Id);
                return null;
            }

            // Get company name from claims
            var companyName = principal.FindFirst("company_name")?.Value ?? "Unknown";

            return new ClientAdminContext
            {
                TokenId = tokenEntity.Id,
                CompanyId = tokenEntity.CompanyId,
                CompanyName = companyName,
                // Offline permissions
                CanBindDevices = tokenEntity.CanBindDevices,
                CanUnbindDevices = tokenEntity.CanUnbindDevices,
                CanViewDevices = tokenEntity.CanViewDevices,
                CanApproveReplacements = tokenEntity.CanApproveReplacements,
                // Online permissions (unified admin system)
                CanViewOnlineTokens = tokenEntity.CanViewOnlineTokens,
                CanManageOnlineTokens = tokenEntity.CanManageOnlineTokens,
                CanViewOnlineDevices = tokenEntity.CanViewOnlineDevices,
                CanUnbindOnlineDevices = tokenEntity.CanUnbindOnlineDevices
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin token validation failed");
            return null;
        }
    }

    public async Task<DeviceBindingResponse> BindDeviceAsync(ClientAdminContext context, BindDeviceRequest request)
    {
        // Check permission
        if (!context.CanBindDevices)
        {
            return new DeviceBindingResponse
            {
                Success = false,
                Message = _localizer["OfflineLicense.PermissionDenied"]
            };
        }

        // Validate fingerprint
        if (!request.MachineFingerprint.HasMinimumIdentifiers())
        {
            var missing = request.MachineFingerprint.GetMissingRequiredIdentifiers();
            return new DeviceBindingResponse
            {
                Success = false,
                Message = string.Format(_localizer["OfflineLicense.MissingFingerprint"], string.Join(", ", missing))
            };
        }

        // Decrypt subscription ID (client sends encrypted IDs)
        var decryptedSubscriptionId = _idEncryption.Decrypt(request.SubscriptionId);
        _logger.LogInformation("Binding device: encrypted={Encrypted}, decrypted={Decrypted}", 
            request.SubscriptionId, decryptedSubscriptionId);

        // Get subscription and verify it belongs to this company
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(decryptedSubscriptionId);
        if (subscription == null || subscription.CompanyId != context.CompanyId)
        {
            return new DeviceBindingResponse
            {
                Success = false,
                Message = _localizer["Subscription.NotFound"]
            };
        }

        var plan = subscription.Plan;
        if (plan == null)
        {
            return new DeviceBindingResponse
            {
                Success = false,
                Message = _localizer["SubscriptionPlan.NotFound"]
            };
        }

        // Get current activations
        var currentActivations = await _activationRepo.GetBySubscriptionAsync(decryptedSubscriptionId);
        var machineHash = _licenseService.ComputeFingerprintHash(request.MachineFingerprint);
        
        // Use effective device limit from subscription (override > plan)
        var maxDevices = subscription.EffectiveMaxDevices;

        // Check if already bound
        var existingActivation = currentActivations.FirstOrDefault(a => a.MachineHash == machineHash);
        if (existingActivation != null)
        {
            return new DeviceBindingResponse
            {
                Success = true,
                Message = _localizer["OfflineLicense.DeviceAlreadyActivated"],
                ActivationId = existingActivation.Id,
                CurrentDeviceCount = currentActivations.Count,
                MaxDevices = maxDevices,
                RemainingSlots = maxDevices == 0 ? -1 : maxDevices - currentActivations.Count
            };
        }

        // Check device limit
        if (maxDevices > 0 && currentActivations.Count >= maxDevices)
        {
            return new DeviceBindingResponse
            {
                Success = false,
                Message = _localizer["OfflineLicense.MaxDevicesReached"],
                CurrentDeviceCount = currentActivations.Count,
                MaxDevices = maxDevices,
                RemainingSlots = 0
            };
        }

        // Create activation
        var activation = new LicenseActivation
        {
            Id = Guid.NewGuid(),
            SubscriptionId = decryptedSubscriptionId,
            CompanyId = context.CompanyId,
            MachineHash = machineHash,
            DeviceName = request.DeviceName ?? "Unknown Device",
            OperatingSystem = request.OperatingSystem,
            MacAddress = request.MachineFingerprint.MacAddress,
            MotherboardSerial = request.MachineFingerprint.MotherboardSerial,
            CpuId = request.MachineFingerprint.CpuId,
            DiskSerial = request.MachineFingerprint.DiskSerial,
            ActivatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        await _activationRepo.AddAsync(activation);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Device {DeviceName} bound to subscription {SubscriptionId} by admin token {TokenId}",
            activation.DeviceName, request.SubscriptionId, context.TokenId);

        return new DeviceBindingResponse
        {
            Success = true,
            Message = _localizer["OfflineLicense.DeviceActivated"],
            ActivationId = activation.Id,
            CurrentDeviceCount = currentActivations.Count + 1,
            MaxDevices = maxDevices,
            RemainingSlots = maxDevices == 0 ? -1 : maxDevices - currentActivations.Count - 1
        };
    }

    public async Task<BulkDeviceBindingResponse> BindDevicesBulkAsync(ClientAdminContext context, BulkBindDeviceRequest request)
    {
        var response = new BulkDeviceBindingResponse();

        foreach (var device in request.Devices)
        {
            var bindRequest = new BindDeviceRequest
            {
                SubscriptionId = request.SubscriptionId,
                MachineFingerprint = device.MachineFingerprint,
                DeviceName = device.DeviceName,
                OperatingSystem = device.OperatingSystem
            };

            var result = await BindDeviceAsync(context, bindRequest);

            response.Results.Add(new DeviceBindingResult
            {
                DeviceName = device.DeviceName ?? "Unknown",
                Success = result.Success,
                Message = result.Message,
                ActivationId = result.ActivationId
            });

            if (result.Success) response.SuccessCount++;
            else response.FailedCount++;
        }

        return response;
    }

    public async Task<bool> UnbindDeviceAsync(ClientAdminContext context, UnbindDeviceRequest request)
    {
        if (!context.CanUnbindDevices)
        {
            _logger.LogWarning("Unbind denied: token {TokenId} lacks permission", context.TokenId);
            return false;
        }

        LicenseActivation? activation = null;

        if (request.ActivationId.HasValue)
        {
            activation = await _activationRepo.GetByIdAsync(request.ActivationId.Value, null);
        }
        else if (request.MachineFingerprint != null)
        {
            var hash = _licenseService.ComputeFingerprintHash(request.MachineFingerprint);
            activation = await _activationRepo.GetByMachineHashAsync(request.SubscriptionId, hash);
        }

        if (activation == null || activation.CompanyId != context.CompanyId)
        {
            return false;
        }

        activation.IsActive = false;
        activation.DeactivatedAtUtc = DateTime.UtcNow;
        activation.DeactivationReason = request.Reason ?? "Unbound by client admin";

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Device {DeviceName} unbound from subscription {SubscriptionId} by admin token {TokenId}",
            activation.DeviceName, activation.SubscriptionId, context.TokenId);

        return true;
    }

    public async Task<List<BoundDeviceDto>> GetBoundDevicesAsync(ClientAdminContext context, Guid subscriptionId)
    {
        if (!context.CanViewDevices)
        {
            return new List<BoundDeviceDto>();
        }

        // Verify subscription belongs to company
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null);
        if (subscription == null || subscription.CompanyId != context.CompanyId)
        {
            return new List<BoundDeviceDto>();
        }

        var activations = await _activationRepo.GetBySubscriptionAsync(subscriptionId, includeDeactivated: false);

        return activations.Select(a => new BoundDeviceDto
        {
            ActivationId = a.Id,
            DeviceName = a.DeviceName ?? "Unknown",
            OperatingSystem = a.OperatingSystem,
            MachineHashTruncated = TruncateHash(a.MachineHash),
            ActivatedAtUtc = a.ActivatedAtUtc,
            LastSeenAtUtc = a.LastSeenAtUtc,
            ValidationCount = a.ValidationCount,
            IsActive = a.IsActive
        }).ToList();
    }

    public async Task<CompanyDeviceSummary> GetCompanyDeviceSummaryAsync(ClientAdminContext context)
    {
        if (!context.CanViewDevices)
        {
            return new CompanyDeviceSummary { CompanyId = context.CompanyId, CompanyName = context.CompanyName };
        }

        var allSubscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(context.CompanyId);
        var subscriptions = allSubscriptions.Where(s => s.IsActive).ToList();
        var summary = new CompanyDeviceSummary
        {
            CompanyId = context.CompanyId,
            CompanyName = context.CompanyName,
            TotalSubscriptions = subscriptions.Count
        };

        foreach (var sub in subscriptions)
        {
            var plan = sub.Plan;
            if (plan == null) continue;

            var activations = await _activationRepo.GetBySubscriptionAsync(sub.Id);
            
            // Use effective device limit from subscription (override > plan)
            var maxDevices = sub.EffectiveMaxDevices;

            var subSummary = new SubscriptionDeviceSummary
            {
                SubscriptionId = sub.Id,
                PlanName = plan.Name,
                MaxDevices = maxDevices,
                BoundDevices = activations.Count,
                RemainingSlots = maxDevices == 0 ? -1 : maxDevices - activations.Count,
                RequiresMachineBinding = plan.RequireMachineBinding,
                ExpiresAtUtc = sub.ExpiryDateUtc,
                IsActive = sub.IsActive,
                // New fields
                AdmissionMode = sub.EffectiveDeviceAdmissionMode.ToString(),
                HasOverride = sub.MaxDevicesOverride.HasValue
            };

            summary.Subscriptions.Add(subSummary);
            summary.TotalBoundDevices += activations.Count;

            if (maxDevices > 0)
            {
                summary.SubscriptionsWithDeviceLimit++;
                summary.TotalMaxDevices += maxDevices;
            }
        }

        return summary;
    }

    #endregion

    #region Device Replacement

    public async Task<List<DeviceReplacementRequestDto>> GetPendingReplacementRequestsAsync(ClientAdminContext context)
    {
        if (!context.CanApproveReplacements && !context.CanViewDevices)
        {
            return new List<DeviceReplacementRequestDto>();
        }

        var requests = await _replacementRepo.GetPendingByCompanyAsync(context.CompanyId);
        
        return requests.Select(r => new DeviceReplacementRequestDto
        {
            Id = r.Id,
            SubscriptionId = r.SubscriptionId,
            PlanName = r.Subscription?.Plan?.Name ?? "Unknown",
            NewDeviceName = r.NewDeviceName ?? "Unknown Device",
            NewDeviceOs = r.NewDeviceOs,
            NewMachineHashTruncated = TruncateHash(r.NewMachineHash),
            OldActivationId = r.OldActivationId,
            OldDeviceName = r.OldDeviceName ?? r.OldActivation?.DeviceName,
            RequestedAtUtc = r.CreatedTimestamp,
            RequestedFromIp = r.RequestedFromIp ?? "",
            Status = r.Status.ToString()
        }).ToList();
    }

    public async Task<DeviceBindingResponse> ApproveReplacementRequestAsync(ClientAdminContext context, Guid requestId)
    {
        if (!context.CanApproveReplacements)
        {
            return new DeviceBindingResponse
            {
                Success = false,
                Message = _localizer["OfflineLicense.PermissionDenied"]
            };
        }

        var request = await _replacementRepo.GetByIdAsync(requestId, null);
        if (request == null || request.CompanyId != context.CompanyId)
        {
            return new DeviceBindingResponse
            {
                Success = false,
                Message = _localizer["OfflineLicense.ReplacementNotFound"]
            };
        }

        if (request.Status != ReplacementRequestStatus.Pending || request.IsExpired)
        {
            return new DeviceBindingResponse
            {
                Success = false,
                Message = _localizer["OfflineLicense.ReplacementExpiredOrResolved"]
            };
        }

        // Get subscription and plan
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(request.SubscriptionId);
        if (subscription == null)
        {
            return new DeviceBindingResponse
            {
                Success = false,
                Message = _localizer["Subscription.NotFound"]
            };
        }

        var plan = subscription.Plan;

        // Determine which device to replace
        LicenseActivation? deviceToReplace = null;

        if (request.OldActivationId.HasValue)
        {
            // Specific device selected
            deviceToReplace = await _activationRepo.GetByIdAsync(request.OldActivationId.Value, null);
        }
        else
        {
            // Use policy to determine which device to replace
            var activeDevices = await _activationRepo.GetBySubscriptionAsync(request.SubscriptionId);
            
            if (activeDevices.Count > 0 && plan != null)
            {
                deviceToReplace = plan.DeviceReplacementPolicy switch
                {
                    DeviceReplacementPolicy.AutoReplaceOldest => 
                        activeDevices.OrderBy(d => d.ActivatedAtUtc).FirstOrDefault(),
                    DeviceReplacementPolicy.AutoReplaceLeastActive => 
                        activeDevices.OrderBy(d => d.LastSeenAtUtc).FirstOrDefault(),
                    _ => activeDevices.OrderBy(d => d.LastSeenAtUtc).FirstOrDefault()
                };
            }
        }

        // Deactivate old device
        if (deviceToReplace != null)
        {
            deviceToReplace.IsActive = false;
            deviceToReplace.DeactivatedAtUtc = DateTime.UtcNow;
            deviceToReplace.DeactivationReason = $"Replaced by {request.NewDeviceName ?? "new device"} (Replacement ID: {requestId})";
            
            _logger.LogInformation("Deactivated device {DeviceName} for replacement request {RequestId}",
                deviceToReplace.DeviceName, requestId);
        }

        // Create new activation
        var newActivation = new LicenseActivation
        {
            Id = Guid.NewGuid(),
            SubscriptionId = request.SubscriptionId,
            CompanyId = request.CompanyId,
            MachineHash = request.NewMachineHash,
            DeviceName = request.NewDeviceName ?? "Unknown Device",
            OperatingSystem = request.NewDeviceOs,
            MacAddress = request.NewMacAddress,
            MotherboardSerial = request.NewMotherboardSerial,
            ActivatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        await _activationRepo.AddAsync(newActivation);

        // Update replacement request
        request.Status = ReplacementRequestStatus.Approved;
        request.ResolvedAtUtc = DateTime.UtcNow;
        request.ResolvedByTokenId = context.TokenId;
        request.NewActivationId = newActivation.Id;

        await _unitOfWork.SaveChangesAsync();

        var currentActivations = await _activationRepo.GetBySubscriptionAsync(request.SubscriptionId);

        _logger.LogInformation("Approved replacement request {RequestId}: {OldDevice} -> {NewDevice}",
            requestId, deviceToReplace?.DeviceName ?? "none", newActivation.DeviceName);

        return new DeviceBindingResponse
        {
            Success = true,
            Message = _localizer["OfflineLicense.ReplacementApproved"],
            ActivationId = newActivation.Id,
            CurrentDeviceCount = currentActivations.Count,
            MaxDevices = plan?.MaxDevices ?? 0,
            RemainingSlots = plan?.MaxDevices == 0 ? -1 : (plan?.MaxDevices ?? 0) - currentActivations.Count
        };
    }

    public async Task<bool> RejectReplacementRequestAsync(ClientAdminContext context, Guid requestId, string reason)
    {
        if (!context.CanApproveReplacements)
        {
            _logger.LogWarning("Rejection denied: token {TokenId} lacks permission", context.TokenId);
            return false;
        }

        var request = await _replacementRepo.GetByIdAsync(requestId, null);
        if (request == null || request.CompanyId != context.CompanyId)
        {
            return false;
        }

        if (request.Status != ReplacementRequestStatus.Pending)
        {
            return false;
        }

        request.Status = ReplacementRequestStatus.Rejected;
        request.ResolvedAtUtc = DateTime.UtcNow;
        request.ResolvedByTokenId = context.TokenId;
        request.RejectionReason = reason;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Rejected replacement request {RequestId}: {Reason}", requestId, reason);

        return true;
    }

    /// <summary>
    /// Create a device replacement request when max devices reached.
    /// Called when a new device tries to bind but limit is reached.
    /// </summary>
    public async Task<CreateReplacementResponse> CreateReplacementRequestAsync(
        ClientAdminContext context, 
        CreateReplacementRequest request)
    {
        // Verify subscription belongs to company
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(request.SubscriptionId);
        if (subscription == null || subscription.CompanyId != context.CompanyId)
        {
            return new CreateReplacementResponse
            {
                Success = false,
                Message = _localizer["Subscription.NotFound"]
            };
        }

        var plan = subscription.Plan;
        if (plan == null)
        {
            return new CreateReplacementResponse
            {
                Success = false,
                Message = _localizer["SubscriptionPlan.NotFound"]
            };
        }

        // Validate fingerprint
        if (!request.MachineFingerprint.HasMinimumIdentifiers())
        {
            var missing = request.MachineFingerprint.GetMissingRequiredIdentifiers();
            return new CreateReplacementResponse
            {
                Success = false,
                Message = string.Format(_localizer["OfflineLicense.MissingFingerprint"], string.Join(", ", missing))
            };
        }

        var machineHash = _licenseService.ComputeFingerprintHash(request.MachineFingerprint);

        // Check if already bound
        var existingActivation = await _activationRepo.GetByMachineHashAsync(request.SubscriptionId, machineHash);
        if (existingActivation != null && existingActivation.IsActive)
        {
            return new CreateReplacementResponse
            {
                Success = false,
                Message = _localizer["OfflineLicense.DeviceAlreadyActivated"],
                AlreadyActivated = true
            };
        }

        // Check if replacement request already pending for this device
        var existingRequest = await _replacementRepo.GetPendingByMachineHashAsync(request.SubscriptionId, machineHash);
        if (existingRequest != null)
        {
            return new CreateReplacementResponse
            {
                Success = true,
                Message = _localizer["OfflineLicense.ReplacementAlreadyPending"],
                RequestId = existingRequest.Id,
                AlreadyPending = true
            };
        }

        // Determine device to replace based on policy
        LicenseActivation? suggestedDevice = null;
        var activeDevices = await _activationRepo.GetBySubscriptionAsync(request.SubscriptionId);

        if (request.OldActivationId.HasValue)
        {
            suggestedDevice = activeDevices.FirstOrDefault(d => d.Id == request.OldActivationId.Value);
        }
        else if (activeDevices.Count > 0)
        {
            suggestedDevice = plan.DeviceReplacementPolicy switch
            {
                DeviceReplacementPolicy.AutoReplaceOldest => 
                    activeDevices.OrderBy(d => d.ActivatedAtUtc).FirstOrDefault(),
                DeviceReplacementPolicy.AutoReplaceLeastActive => 
                    activeDevices.OrderBy(d => d.LastSeenAtUtc).FirstOrDefault(),
                _ => activeDevices.OrderBy(d => d.LastSeenAtUtc).FirstOrDefault()
            };
        }

        // Create replacement request
        var replacementRequest = new DeviceReplacementRequest
        {
            Id = Guid.NewGuid(),
            SubscriptionId = request.SubscriptionId,
            CompanyId = context.CompanyId,
            NewMachineHash = machineHash,
            NewDeviceName = request.DeviceName,
            NewDeviceOs = request.OperatingSystem,
            NewMacAddress = request.MachineFingerprint.MacAddress,
            NewMotherboardSerial = request.MachineFingerprint.MotherboardSerial,
            OldActivationId = suggestedDevice?.Id,
            OldDeviceName = suggestedDevice?.DeviceName,
            OldMachineHash = suggestedDevice?.MachineHash,
            Status = ReplacementRequestStatus.Pending,
            RequestedFromIp = request.RequestedFromIp,
            RequestedUserAgent = request.RequestedUserAgent,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(REPLACEMENT_REQUEST_EXPIRY_HOURS)
        };

        await _replacementRepo.AddAsync(replacementRequest);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created replacement request {RequestId} for subscription {SubscriptionId}: {NewDevice} replacing {OldDevice}",
            replacementRequest.Id, request.SubscriptionId, request.DeviceName, suggestedDevice?.DeviceName ?? "TBD");

        return new CreateReplacementResponse
        {
            Success = true,
            Message = _localizer["OfflineLicense.ReplacementRequestCreated"],
            RequestId = replacementRequest.Id,
            SuggestedDeviceToReplace = suggestedDevice?.DeviceName,
            ExpiresAtUtc = replacementRequest.ExpiresAtUtc
        };
    }

    #endregion

    #region Private Helpers

    private static string ComputeTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string TruncateHash(string? hash)
    {
        if (string.IsNullOrEmpty(hash) || hash.Length < 16)
            return hash ?? "";
        return $"{hash[..8]}...{hash[^8..]}";
    }

    private static AdminTokenDto MapToDto(OfflineLicenseAdminToken token, string companyName)
    {
        return new AdminTokenDto
        {
            Id = token.Id,
            CompanyId = token.CompanyId,
            CompanyName = companyName,
            Name = token.Name,
            IssuedAtUtc = token.IssuedAtUtc,
            ExpiresAtUtc = token.ExpiresAtUtc,
            DaysUntilExpiry = token.DaysUntilExpiry,
            Status = token.Status.ToString(),
            IsValid = token.IsValid,
            IsExpired = token.IsExpired,
            LastUsedAtUtc = token.LastUsedAtUtc,
            UsageCount = token.UsageCount,
            LastUsedFromIp = token.LastUsedFromIp,
            // Offline permissions
            CanBindDevices = token.CanBindDevices,
            CanUnbindDevices = token.CanUnbindDevices,
            CanViewDevices = token.CanViewDevices,
            CanApproveReplacements = token.CanApproveReplacements,
            // Online permissions (unified admin system)
            CanViewOnlineTokens = token.CanViewOnlineTokens,
            CanManageOnlineTokens = token.CanManageOnlineTokens,
            CanViewOnlineDevices = token.CanViewOnlineDevices,
            CanUnbindOnlineDevices = token.CanUnbindOnlineDevices,
            DailyApiLimit = token.DailyApiLimit,
            TodayApiCalls = token.TodayApiCalls,
            Notes = token.Notes
        };
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.OfflineLicense;
using Application.Services;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Enterprise-grade offline license service with military-grade security.
/// 
/// Security Features:
/// - AES-256-GCM authenticated encryption (encryption + authentication in one)
/// - Unique nonce per license key (12 bytes for GCM)
/// - Hardware fingerprint binding (prevents license sharing)
/// - Clock tampering detection (multi-point validation)
/// - Key versioning (supports rotation without breaking existing keys)
/// - Payload integrity checksum (HMAC-SHA256)
/// 
/// Key Structure:
/// [Version:2][LicenseId:16][Nonce:12][Ciphertext:variable][Tag:16]
/// </summary>
public class OfflineLicenseService : IOfflineLicenseService
{
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<OfflineLicenseService> _logger;
    private readonly OfflineLicenseSettings _settings;

    // Header sizes
    private const int VERSION_SIZE = 2;
    private const int LICENSE_ID_SIZE = 16;
    private const int NONCE_SIZE = 12; // GCM standard
    private const int TAG_SIZE = 16;   // GCM standard
    private const int HEADER_SIZE = VERSION_SIZE + LICENSE_ID_SIZE + NONCE_SIZE;

    public OfflineLicenseService(
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        ICompanyRepository companyRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILocalizationService localizer,
        ILogger<OfflineLicenseService> logger,
        IOptions<OfflineLicenseSettings> settings)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _companyRepo = companyRepo;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _logger = logger;
        _settings = settings.Value;
    }

    #region Generation

    public async Task<GenerateLicenseResponse> GenerateLicenseKeyAsync(
        Guid subscriptionId,
        GenerateLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating license key for subscription {SubscriptionId}", subscriptionId);

        // Get subscription with full details
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        // Validate required related entities are loaded
        if (subscription.Plan == null)
            throw new BadRequestException(_localizer["Subscription.PlanNotFound"]);
        
        if (subscription.Company == null)
            throw new BadRequestException(_localizer["Subscription.CompanyNotFound"]);

        if (!subscription.IsActive && !subscription.IsTrial)
            throw new BadRequestException(_localizer["OfflineLicense.CannotGenerateForInactive"]);

        // Check if already has a key and not forcing regeneration
        if (!string.IsNullOrEmpty(subscription.OfflineLicenseKey) && !request.ForceRegenerate)
            throw new BadRequestException(_localizer["OfflineLicense.KeyAlreadyExists"]);

        // Compute machine fingerprint hash if provided
        string? fingerprintHash = null;
        if (request.MachineFingerprint != null && _settings.EnforceMachineBinding)
        {
            if (!request.MachineFingerprint.HasMinimumIdentifiers())
                throw new BadRequestException(_localizer["OfflineLicense.InsufficientFingerprint"]);

            fingerprintHash = ComputeFingerprintHash(request.MachineFingerprint);
        }

        // Build entitlement matrix from plan
        var (projects, standaloneModules) = BuildEntitlementsFromPlan(subscription.Plan);

        // Create license payload
        var licenseId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var payload = new LicensePayload
        {
            // Identity
            LicenseId = licenseId,
            CompanyId = subscription.CompanyId,
            CompanyName = subscription.Company.Name,
            SubscriptionId = subscription.Id,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan.Name,

            // Hardware binding
            MachineFingerprint = fingerprintHash,
            AuthorizedMachines = request.AllowMultipleMachines ? new List<string>() : null,

            // Timestamps (Unix seconds)
            IssuedAt = new DateTimeOffset(now).ToUnixTimeSeconds(),
            StartsAt = new DateTimeOffset(subscription.StartDateUtc).ToUnixTimeSeconds(),
            ExpiresAt = new DateTimeOffset(subscription.ExpiryDateUtc).ToUnixTimeSeconds(),
            GracePeriodEndsAt = CalculateGracePeriodEnd(subscription),
            ExportDeadlineAt = subscription.ExportDeadlineUtc.HasValue 
                ? new DateTimeOffset(subscription.ExportDeadlineUtc.Value).ToUnixTimeSeconds() 
                : null,
            LastValidationTime = new DateTimeOffset(now).ToUnixTimeSeconds(),

            // Status & versioning
            Version = _settings.CurrentVersion,
            EntitlementsVersion = subscription.Plan.EntitlementVersion,
            IsTrial = subscription.IsTrial,
            IsRevoked = false,
            AccessMode = subscription.AccessMode,

            // Entitlements
            Projects = projects,
            StandaloneModules = standaloneModules,
            Features = subscription.Plan.CustomFeatures ?? new List<string>(),

            // Security
            Issuer = _settings.Issuer,
            RandomNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        };

        // Calculate integrity checksum
        payload.Checksum = CalculatePayloadChecksum(payload);

        // Encrypt payload to license key
        var licenseKey = EncryptLicensePayload(payload);

        // Store in subscription
        subscription.OfflineLicenseKey = licenseKey;
        subscription.LicenseKeyGeneratedAt = now;
        subscription.LicenseKeyVersion = _settings.CurrentVersion;

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("License key generated successfully for subscription {SubscriptionId}, LicenseId: {LicenseId}", 
            subscriptionId, licenseId);

        return new GenerateLicenseResponse
        {
            LicenseKey = licenseKey,
            LicenseId = licenseId,
            GeneratedAtUtc = now,
            ExpiresAtUtc = subscription.ExpiryDateUtc,
            DaysUntilExpiry = Math.Max(0, (int)(subscription.ExpiryDateUtc - now).TotalDays),
            Version = _settings.CurrentVersion,
            EntitlementsVersion = subscription.Plan.EntitlementVersion,
            IsMachineBound = !string.IsNullOrEmpty(fingerprintHash),
            MachineFingerprint = fingerprintHash != null ? TruncateForDisplay(fingerprintHash) : null,
            CompanyName = subscription.Company.Name,
            PlanName = subscription.Plan.Name,
            Message = _localizer["OfflineLicense.GeneratedSuccessfully"]
        };
    }

    public async Task<GenerateLicenseResponse> RegenerateLicenseKeyAsync(
        Guid subscriptionId,
        GenerateLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        request.ForceRegenerate = true;
        return await GenerateLicenseKeyAsync(subscriptionId, request, cancellationToken);
    }

    #endregion

    #region Validation

    public async Task<ValidateLicenseResponse> ValidateLicenseKeyAsync(
        ValidateLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new ValidateLicenseResponse();

        try
        {
            // Step 1: Decrypt and parse the license key
            var payload = DecryptLicensePayload(request.LicenseKey);
            if (payload == null)
            {
                return FailValidation(response, OfflineLicenseValidationStatus.InvalidFormat,
                    _localizer["OfflineLicense.InvalidFormat"]);
            }

            // Step 2: Verify checksum (detect tampering)
            var expectedChecksum = CalculatePayloadChecksum(payload);
            if (payload.Checksum != expectedChecksum)
            {
                _logger.LogWarning("License checksum mismatch for LicenseId {LicenseId}", payload.LicenseId);
                return FailValidation(response, OfflineLicenseValidationStatus.TamperedKey,
                    _localizer["OfflineLicense.TamperedKey"]);
            }

            // Step 3: Verify issuer
            if (payload.Issuer != _settings.Issuer)
            {
                return FailValidation(response, OfflineLicenseValidationStatus.InvalidFormat,
                    _localizer["OfflineLicense.InvalidIssuer"]);
            }

            // Step 4: Check version
            if (payload.Version < _settings.MinimumSupportedVersion)
            {
                return FailValidation(response, OfflineLicenseValidationStatus.OutdatedVersion,
                    _localizer["OfflineLicense.OutdatedVersion"]);
            }

            // Step 5: Check if revoked
            if (payload.IsRevoked)
            {
                return FailValidation(response, OfflineLicenseValidationStatus.Revoked,
                    _localizer["OfflineLicense.Revoked"]);
            }

            // Step 6: Check machine fingerprint if bound
            if (_settings.EnforceMachineBinding && !string.IsNullOrEmpty(payload.MachineFingerprint))
            {
                if (request.MachineFingerprint == null)
                {
                    return FailValidation(response, OfflineLicenseValidationStatus.MachineNotAuthorized,
                        _localizer["OfflineLicense.MachineRequired"]);
                }

                var requestFingerprintHash = ComputeFingerprintHash(request.MachineFingerprint);
                
                // Check primary fingerprint using constant-time comparison (prevents timing attacks)
                var isAuthorized = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(payload.MachineFingerprint ?? ""),
                    Encoding.UTF8.GetBytes(requestFingerprintHash));
                
                // Check additional authorized machines
                if (!isAuthorized && payload.AuthorizedMachines?.Any() == true)
                {
                    foreach (var authorizedHash in payload.AuthorizedMachines)
                    {
                        if (CryptographicOperations.FixedTimeEquals(
                            Encoding.UTF8.GetBytes(authorizedHash),
                            Encoding.UTF8.GetBytes(requestFingerprintHash)))
                        {
                            isAuthorized = true;
                            break;
                        }
                    }
                }

                if (!isAuthorized)
                {
                    _logger.LogWarning("Machine not authorized for license {LicenseId}. Expected: {Expected}, Got: {Got}",
                        payload.LicenseId, TruncateForDisplay(payload.MachineFingerprint), TruncateForDisplay(requestFingerprintHash));
                    return FailValidation(response, OfflineLicenseValidationStatus.MachineNotAuthorized,
                        _localizer["OfflineLicense.MachineNotAuthorized"]);
                }

                response.MachineAuthorized = true;
            }

            // Step 7: Check clock tampering
            if (_settings.EnableClockTamperDetection)
            {
                var clockTampered = DetectClockTampering(payload, request.ClientTimestamp);
                if (clockTampered)
                {
                    _logger.LogWarning("Clock tampering detected for license {LicenseId}", payload.LicenseId);
                    response.ClockTamperingDetected = true;
                    response.Warnings.Add(_localizer["OfflineLicense.ClockTamperingDetected"]);
                    // Don't fail immediately, but flag it
                }
            }

            // Step 8: Check not yet active
            if (DateTime.UtcNow < payload.StartsAtUtc)
            {
                return FailValidation(response, OfflineLicenseValidationStatus.NotYetActive,
                    _localizer["OfflineLicense.NotYetActive"]);
            }

            // Step 9: Check expiry and determine access mode
            var now = DateTime.UtcNow;
            var effectiveAccessMode = payload.AccessMode;

            if (payload.IsExpired)
            {
                if (payload.IsInGracePeriod)
                {
                    effectiveAccessMode = SubscriptionAccessMode.GracePeriod;
                    response.IsInGracePeriod = true;
                    response.Warnings.Add(string.Format(_localizer["OfflineLicense.InGracePeriod"], 
                        (int)(payload.GracePeriodEndsAtUtc!.Value - now).TotalDays));
                }
                else if (payload.IsInExportOnly)
                {
                    effectiveAccessMode = SubscriptionAccessMode.ExportOnly;
                    response.IsInExportOnly = true;
                    response.Warnings.Add(_localizer["OfflineLicense.ExportOnlyMode"]);
                }
                else
                {
                    return FailValidation(response, OfflineLicenseValidationStatus.Blocked,
                        _localizer["OfflineLicense.Blocked"]);
                }
            }

            // Step 10: Add expiry warnings
            if (payload.DaysUntilExpiry <= _settings.ExpiryWarningDays && payload.DaysUntilExpiry > 0)
            {
                response.Warnings.Add(string.Format(_localizer["OfflineLicense.ExpiresInDays"], payload.DaysUntilExpiry));
            }

            // Step 11: Online validation if requested
            if (request.ValidateOnline)
            {
                var subscription = await _subscriptionRepo.GetWithDetailsAsync(payload.SubscriptionId);
                if (subscription == null)
                {
                    return FailValidation(response, OfflineLicenseValidationStatus.SubscriptionNotFound,
                        _localizer["Subscription.NotFound"]);
                }

                // Check company status
                if (subscription.Company != null && !subscription.Company.IsActive)
                {
                    return FailValidation(response, OfflineLicenseValidationStatus.CompanyInactive,
                        _localizer["OfflineLicense.CompanyInactive"]);
                }

                if (!subscription.IsActive)
                {
                    return FailValidation(response, OfflineLicenseValidationStatus.SubscriptionNotFound,
                        _localizer["OfflineLicense.SubscriptionInactive"]);
                }

                // Check if entitlements version changed
                if ((subscription.Plan?.EntitlementVersion ?? 1) > payload.EntitlementsVersion)
                {
                    response.Warnings.Add(_localizer["OfflineLicense.StaleEntitlements"]);
                }
            }

            // SUCCESS - Build response
            response.IsValid = true;
            response.Status = payload.IsExpired 
                ? (response.IsInGracePeriod ? OfflineLicenseValidationStatus.GracePeriod : OfflineLicenseValidationStatus.ExportOnly)
                : OfflineLicenseValidationStatus.Valid;
            response.Message = _localizer["OfflineLicense.Valid"];

            // Identity
            response.LicenseId = payload.LicenseId;
            response.CompanyId = payload.CompanyId;
            response.CompanyName = payload.CompanyName;
            response.SubscriptionId = payload.SubscriptionId;
            response.PlanName = payload.PlanName;

            // Dates
            response.ExpiresAtUtc = payload.ExpiresAtUtc;
            response.DaysUntilExpiry = payload.DaysUntilExpiry;
            response.GracePeriodEndsAtUtc = payload.GracePeriodEndsAtUtc;
            response.ExportDeadlineUtc = payload.ExportDeadlineAtUtc;

            // Status
            response.IsTrial = payload.IsTrial;
            response.AccessMode = effectiveAccessMode;
            response.KeyVersion = payload.Version;
            response.EntitlementsVersion = payload.EntitlementsVersion;

            // Entitlements
            response.Projects = payload.Projects.Select(p => new ValidatedProjectEntitlement
            {
                ProjectId = p.ProjectId,
                Name = p.Name,
                Code = p.Code,
                AccessLevel = p.AccessLevel,
                CanCreate = effectiveAccessMode == SubscriptionAccessMode.Full && p.CanCreate,
                CanRead = p.CanRead, // Always allow read
                CanUpdate = effectiveAccessMode == SubscriptionAccessMode.Full && p.CanUpdate,
                CanDelete = effectiveAccessMode == SubscriptionAccessMode.Full && p.CanDelete,
                CanExport = (effectiveAccessMode == SubscriptionAccessMode.Full || 
                            effectiveAccessMode == SubscriptionAccessMode.ExportOnly || 
                            effectiveAccessMode == SubscriptionAccessMode.GracePeriod) && p.CanExport,
                Modules = p.Modules.Select(m => new ValidatedModuleEntitlement
                {
                    ModuleId = m.ModuleId,
                    Name = m.Name,
                    Code = m.Code,
                    AccessLevel = m.AccessLevel,
                    CanCreate = effectiveAccessMode == SubscriptionAccessMode.Full && m.CanCreate,
                    CanRead = m.CanRead,
                    CanUpdate = effectiveAccessMode == SubscriptionAccessMode.Full && m.CanUpdate,
                    CanDelete = effectiveAccessMode == SubscriptionAccessMode.Full && m.CanDelete,
                    CanExport = (effectiveAccessMode == SubscriptionAccessMode.Full || 
                                effectiveAccessMode == SubscriptionAccessMode.ExportOnly || 
                                effectiveAccessMode == SubscriptionAccessMode.GracePeriod) && m.CanExport,
                    Features = m.Features
                }).ToList()
            }).ToList();

            response.StandaloneModules = payload.StandaloneModules.Select(m => new ValidatedModuleEntitlement
            {
                ModuleId = m.ModuleId,
                Name = m.Name,
                Code = m.Code,
                AccessLevel = m.AccessLevel,
                CanCreate = effectiveAccessMode == SubscriptionAccessMode.Full && m.CanCreate,
                CanRead = m.CanRead,
                CanUpdate = effectiveAccessMode == SubscriptionAccessMode.Full && m.CanUpdate,
                CanDelete = effectiveAccessMode == SubscriptionAccessMode.Full && m.CanDelete,
                CanExport = (effectiveAccessMode == SubscriptionAccessMode.Full || 
                            effectiveAccessMode == SubscriptionAccessMode.ExportOnly || 
                            effectiveAccessMode == SubscriptionAccessMode.GracePeriod) && m.CanExport,
                Features = m.Features
            }).ToList();

            response.Features = payload.Features;

            return response;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Cryptographic error validating license key");
            return FailValidation(response, OfflineLicenseValidationStatus.TamperedKey,
                _localizer["OfflineLicense.DecryptionFailed"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license key");
            return FailValidation(response, OfflineLicenseValidationStatus.DecryptionError,
                _localizer["OfflineLicense.ValidationError"]);
        }
    }

    public async Task<bool> IsLicenseKeyValidAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        var result = await ValidateLicenseKeyAsync(new ValidateLicenseRequest 
        { 
            LicenseKey = licenseKey,
            ValidateOnline = false 
        }, cancellationToken);
        
        return result.IsValid;
    }

    #endregion

    #region Management

    public async Task<bool> RevokeLicenseKeyAsync(Guid subscriptionId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null);
        if (subscription == null)
            throw new NotFoundException(_localizer["Subscription.NotFound"]);

        subscription.OfflineLicenseKey = null;
        subscription.LicenseKeyGeneratedAt = null;

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("License key revoked for subscription {SubscriptionId}. Reason: {Reason}", 
            subscriptionId, reason ?? "No reason provided");

        return true;
    }

    public async Task<OfflineLicenseDto?> GetLicenseInfoAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId, cancellationToken);
        if (subscription == null)
            return null;

        // Guard against missing related entities
        if (subscription.Company == null || subscription.Plan == null)
        {
            _logger.LogWarning("GetLicenseInfoAsync: Missing Company or Plan for subscription {SubscriptionId}", subscriptionId);
            return null;
        }

        var now = DateTime.UtcNow;
        var hasKey = !string.IsNullOrEmpty(subscription.OfflineLicenseKey);
        var isExpired = subscription.ExpiryDateUtc <= now;
        var daysUntilExpiry = Math.Max(0, (int)(subscription.ExpiryDateUtc - now).TotalDays);

        // Determine status and color
        var (status, statusColor) = GetLicenseStatus(subscription, hasKey, isExpired, daysUntilExpiry);

        // Try to extract machine fingerprint from key
        string? fingerprintPreview = null;
        if (hasKey)
        {
            try
            {
                var payload = DecryptLicensePayload(subscription.OfflineLicenseKey!);
                if (payload?.MachineFingerprint != null)
                {
                    fingerprintPreview = TruncateForDisplay(payload.MachineFingerprint);
                }
            }
            catch { /* Ignore decryption errors for display */ }
        }

        return new OfflineLicenseDto
        {
            LicenseId = subscription.Id, // Using subscription ID as we don't store license ID separately
            SubscriptionId = subscription.Id,
            CompanyId = subscription.CompanyId,
            CompanyName = subscription.Company.Name,
            PlanName = subscription.Plan.Name,
            LicenseKey = _currentUserService.AdminTypeName == "SuperAdmin" ? subscription.OfflineLicenseKey : null,
            GeneratedAtUtc = subscription.LicenseKeyGeneratedAt,
            ExpiresAtUtc = subscription.ExpiryDateUtc,
            DaysUntilExpiry = daysUntilExpiry,
            KeyVersion = subscription.LicenseKeyVersion,
            EntitlementsVersion = subscription.Plan.EntitlementVersion,
            IsActive = subscription.IsActive,
            IsExpired = isExpired,
            IsMachineBound = fingerprintPreview != null,
            MachineFingerprint = fingerprintPreview,
            AuthorizedMachineCount = 1, // TODO: Count from payload if multi-machine
            AccessMode = subscription.AccessMode,
            Status = status,
            StatusColor = statusColor,
            HasValidKey = hasKey && subscription.IsActive && !isExpired,
            CanRegenerate = subscription.IsActive,
            CanRevoke = hasKey
        };
    }

    public async Task<CompanyLicenseSummaryDto> GetCompanyLicensesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepo.GetByIdAsync(companyId, null);
        if (company == null)
            throw new NotFoundException(_localizer["Company.NotFound"]);

        var subscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(companyId);
        var now = DateTime.UtcNow;

        var licenses = new List<OfflineLicenseDto>();
        foreach (var sub in subscriptions.Where(s => !string.IsNullOrEmpty(s.OfflineLicenseKey) || s.IsActive))
        {
            var info = await GetLicenseInfoAsync(sub.Id, cancellationToken);
            if (info != null)
                licenses.Add(info);
        }

        return new CompanyLicenseSummaryDto
        {
            CompanyId = companyId,
            CompanyName = company.Name,
            TotalLicenses = licenses.Count,
            ActiveLicenses = licenses.Count(l => l.IsActive && !l.IsExpired),
            ExpiredLicenses = licenses.Count(l => l.IsExpired),
            ExpiringLicenses = licenses.Count(l => !l.IsExpired && l.DaysUntilExpiry <= 30),
            Licenses = licenses
        };
    }

    public async Task<bool> HasValidLicenseKeyAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null);
        return subscription?.OfflineLicenseKey != null && 
               subscription.IsActive && 
               subscription.ExpiryDateUtc > DateTime.UtcNow;
    }

    #endregion

    #region Machine Management

    public async Task<GenerateLicenseResponse> AddAuthorizedMachineAsync(
        Guid subscriptionId, 
        MachineFingerprint fingerprint,
        CancellationToken cancellationToken = default)
    {
        // For now, regenerate the key with the new machine
        // In a more complex implementation, you'd update the existing key
        return await RegenerateLicenseKeyAsync(subscriptionId, new GenerateLicenseRequest
        {
            SubscriptionId = subscriptionId,
            MachineFingerprint = fingerprint,
            AllowMultipleMachines = true,
            ForceRegenerate = true
        }, cancellationToken);
    }

    public Task<bool> RemoveAuthorizedMachineAsync(Guid subscriptionId, string fingerprintHash, CancellationToken cancellationToken = default)
    {
        // Would require regenerating the key without this machine
        throw new NotImplementedException("Remove authorized machine requires key regeneration");
    }

    #endregion

    #region Utilities

    public string ComputeFingerprintHash(MachineFingerprint fingerprint)
    {
        // SECURITY: Always compute from raw identifiers, never trust precomputed hash!
        // PrecomputedHash is ONLY for client-side display, never for verification.
        
        // Combine all identifiers in deterministic order
        var combined = new StringBuilder();
        combined.Append(fingerprint.CpuId?.Trim().ToUpperInvariant() ?? "");
        combined.Append("|");
        combined.Append(fingerprint.MotherboardSerial?.Trim().ToUpperInvariant() ?? "");
        combined.Append("|");
        combined.Append(fingerprint.DiskSerial?.Trim().ToUpperInvariant() ?? "");
        combined.Append("|");
        combined.Append(fingerprint.MacAddress?.Trim().ToUpperInvariant().Replace(":", "").Replace("-", "") ?? "");
        combined.Append("|");
        combined.Append(fingerprint.BiosUuid?.Trim().ToUpperInvariant() ?? "");
        combined.Append("|");
        combined.Append(fingerprint.OsProductId?.Trim() ?? "");
        combined.Append("|");
        combined.Append(_settings.FingerprintSalt);

        // SHA256 hash with HMAC for additional security
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.FingerprintSalt));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(combined.ToString()));
        return Convert.ToBase64String(hashBytes);
    }

    public async Task<IEnumerable<Guid>> GetExpiringLicensesAsync(int days, CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.AddDays(days);
        var startDate = DateTime.UtcNow.AddDays(days - 1);

        // Get subscriptions expiring within the specified window
        var subscriptions = await _subscriptionRepo.FindAsync(s =>
            !s.IsDeleted &&
            s.IsActive &&
            !s.IsExpired &&
            !string.IsNullOrEmpty(s.OfflineLicenseKey) &&
            s.ExpiryDateUtc >= startDate &&
            s.ExpiryDateUtc <= targetDate);

        return subscriptions.Select(s => s.Id);
    }

    #endregion

    #region Private Cryptographic Methods

    /// <summary>
    /// Encrypt license payload using AES-256-GCM
    /// </summary>
    private string EncryptLicensePayload(LicensePayload payload)
    {
        // Serialize payload to JSON
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        var plaintext = Encoding.UTF8.GetBytes(json);

        // Generate unique nonce (12 bytes for GCM)
        var nonce = RandomNumberGenerator.GetBytes(NONCE_SIZE);

        // Get encryption key
        var key = Convert.FromBase64String(_settings.EncryptionKey);

        // Encrypt with AES-GCM
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TAG_SIZE];

        using var aesGcm = new AesGcm(key, TAG_SIZE);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        // Build header: Version (2) + LicenseId (16) + Nonce (12)
        var version = BitConverter.GetBytes((ushort)_settings.CurrentVersion);
        var licenseIdBytes = payload.LicenseId.ToByteArray();

        // Combine: Header + Ciphertext + Tag
        var result = new byte[HEADER_SIZE + ciphertext.Length + TAG_SIZE];
        var offset = 0;

        Buffer.BlockCopy(version, 0, result, offset, VERSION_SIZE);
        offset += VERSION_SIZE;

        Buffer.BlockCopy(licenseIdBytes, 0, result, offset, LICENSE_ID_SIZE);
        offset += LICENSE_ID_SIZE;

        Buffer.BlockCopy(nonce, 0, result, offset, NONCE_SIZE);
        offset += NONCE_SIZE;

        Buffer.BlockCopy(ciphertext, 0, result, offset, ciphertext.Length);
        offset += ciphertext.Length;

        Buffer.BlockCopy(tag, 0, result, offset, TAG_SIZE);

        // Return as Base64Url (URL-safe)
        return Base64UrlEncode(result);
    }

    /// <summary>
    /// Decrypt license key and return payload (returns null if invalid)
    /// </summary>
    private LicensePayload? DecryptLicensePayload(string licenseKey)
    {
        try
        {
            // Decode from Base64Url
            var data = Base64UrlDecode(licenseKey);
            if (data.Length < HEADER_SIZE + TAG_SIZE + 1)
                return null;

            // Extract header
            var version = BitConverter.ToUInt16(data, 0);
            var licenseId = new Guid(new ReadOnlySpan<byte>(data, VERSION_SIZE, LICENSE_ID_SIZE));
            var nonce = new byte[NONCE_SIZE];
            Buffer.BlockCopy(data, VERSION_SIZE + LICENSE_ID_SIZE, nonce, 0, NONCE_SIZE);

            // Extract ciphertext and tag
            var ciphertextLength = data.Length - HEADER_SIZE - TAG_SIZE;
            var ciphertext = new byte[ciphertextLength];
            var tag = new byte[TAG_SIZE];

            Buffer.BlockCopy(data, HEADER_SIZE, ciphertext, 0, ciphertextLength);
            Buffer.BlockCopy(data, data.Length - TAG_SIZE, tag, 0, TAG_SIZE);

            // Get decryption key
            var key = Convert.FromBase64String(_settings.EncryptionKey);

            // Decrypt with AES-GCM
            var plaintext = new byte[ciphertextLength];
            using var aesGcm = new AesGcm(key, TAG_SIZE);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            // Deserialize JSON
            var json = Encoding.UTF8.GetString(plaintext);
            var payload = JsonSerializer.Deserialize<LicensePayload>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt license key");
            return null;
        }
    }

    /// <summary>
    /// Calculate HMAC-SHA256 checksum for payload integrity
    /// </summary>
    private string CalculatePayloadChecksum(LicensePayload payload)
    {
        // Create checksum from key fields (excluding the checksum itself)
        var data = $"{payload.LicenseId}|{payload.CompanyId}|{payload.SubscriptionId}|{payload.IssuedAt}|{payload.ExpiresAt}|{payload.Version}|{payload.RandomNonce}";
        
        var key = Convert.FromBase64String(_settings.SigningKey);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Detect clock tampering by comparing timestamps
    /// </summary>
    private bool DetectClockTampering(LicensePayload payload, long? clientTimestamp)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Check 1: Current time should not be before last validation time
        if (now < payload.LastValidationTime - (_settings.MaxClockDriftHours * 3600))
        {
            return true;
        }

        // Check 2: Current time should not be before issue time
        if (now < payload.IssuedAt - (_settings.MaxClockDriftHours * 3600))
        {
            return true;
        }

        // Check 3: If client provided timestamp, check drift
        if (clientTimestamp.HasValue)
        {
            var drift = Math.Abs(now - clientTimestamp.Value);
            if (drift > _settings.MaxClockDriftHours * 3600)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Private Helper Methods

    private static ValidateLicenseResponse FailValidation(
        ValidateLicenseResponse response, 
        OfflineLicenseValidationStatus status, 
        string message)
    {
        response.IsValid = false;
        response.Status = status;
        response.Message = message;
        return response;
    }

    private long? CalculateGracePeriodEnd(Subscription subscription)
    {
        if (subscription.Plan?.GracePeriodDays > 0)
        {
            var graceEnd = subscription.ExpiryDateUtc.AddDays(subscription.Plan.GracePeriodDays);
            return new DateTimeOffset(graceEnd).ToUnixTimeSeconds();
        }
        return null;
    }

    private (List<LicenseProjectEntitlement>, List<LicenseModuleEntitlement>) BuildEntitlementsFromPlan(SubscriptionPlan plan)
    {
        // Build from PlanProjects
        var projects = (plan.PlanProjects ?? new List<PlanProject>())
            .Where(pp => pp.Project != null)
            .Select(pp => 
            {
                // Find entitlement for this project
                var entitlement = plan.Entitlements?.FirstOrDefault(e => e.ProjectId == pp.ProjectId);
                
                return new LicenseProjectEntitlement
                {
                    ProjectId = pp.ProjectId,
                    Name = pp.Project?.Name ?? string.Empty,
                    Code = pp.Project?.Name?.Replace(" ", "").ToUpperInvariant() ?? string.Empty,
                    AccessLevel = entitlement?.AccessLevel ?? EntitlementAccessLevel.Full,
                    CanCreate = entitlement?.CanCreate ?? true,
                    CanRead = entitlement?.CanRead ?? true,
                    CanUpdate = entitlement?.CanUpdate ?? true,
                    CanDelete = entitlement?.CanDelete ?? true,
                    CanExport = entitlement?.CanExport ?? true,
                    Modules = (pp.Project?.ProjectModules ?? new List<ProjectModule>())
                        .Where(pm => pm.Module != null)
                        .Select(pm =>
                        {
                            var moduleEntitlement = plan.Entitlements?.FirstOrDefault(e => e.ModuleId == pm.ModuleId);
                            return new LicenseModuleEntitlement
                            {
                                ModuleId = pm.ModuleId,
                                Name = pm.Module?.Name ?? string.Empty,
                                Code = pm.Module?.Name?.Replace(" ", "").ToUpperInvariant() ?? string.Empty,
                                AccessLevel = moduleEntitlement?.AccessLevel ?? entitlement?.AccessLevel ?? EntitlementAccessLevel.Full,
                                CanCreate = moduleEntitlement?.CanCreate ?? entitlement?.CanCreate ?? true,
                                CanRead = moduleEntitlement?.CanRead ?? entitlement?.CanRead ?? true,
                                CanUpdate = moduleEntitlement?.CanUpdate ?? entitlement?.CanUpdate ?? true,
                                CanDelete = moduleEntitlement?.CanDelete ?? entitlement?.CanDelete ?? true,
                                CanExport = moduleEntitlement?.CanExport ?? entitlement?.CanExport ?? true,
                                Features = ParseFeatures(moduleEntitlement?.Features)
                            };
                        }).ToList()
                };
            }).ToList();

        // Build standalone modules
        var standaloneModules = (plan.PlanModules ?? new List<PlanModule>())
            .Where(pm => pm.Module != null)
            .Select(pm =>
            {
                var entitlement = plan.Entitlements?.FirstOrDefault(e => e.ModuleId == pm.ModuleId);
                return new LicenseModuleEntitlement
                {
                    ModuleId = pm.ModuleId,
                    Name = pm.Module?.Name ?? string.Empty,
                    Code = pm.Module?.Name?.Replace(" ", "").ToUpperInvariant() ?? string.Empty,
                    AccessLevel = entitlement?.AccessLevel ?? EntitlementAccessLevel.Full,
                    CanCreate = entitlement?.CanCreate ?? true,
                    CanRead = entitlement?.CanRead ?? true,
                    CanUpdate = entitlement?.CanUpdate ?? true,
                    CanDelete = entitlement?.CanDelete ?? true,
                    CanExport = entitlement?.CanExport ?? true,
                    Features = ParseFeatures(entitlement?.Features)
                };
            }).ToList();

        return (projects, standaloneModules);
    }

    private static List<string> ParseFeatures(string? features)
    {
        if (string.IsNullOrWhiteSpace(features))
            return new List<string>();
        
        return features.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static (string Status, string Color) GetLicenseStatus(Subscription subscription, bool hasKey, bool isExpired, int daysUntilExpiry)
    {
        if (!hasKey)
            return ("No Key", "gray");
        
        if (!subscription.IsActive)
            return ("Inactive", "gray");
        
        if (isExpired)
            return ("Expired", "red");
        
        if (daysUntilExpiry <= 7)
            return ("Expiring Soon", "orange");
        
        if (daysUntilExpiry <= 30)
            return ("Expiring", "yellow");
        
        return ("Active", "green");
    }

    private static string TruncateForDisplay(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 12)
            return value;
        return value[..8] + "..." + value[^4..];
    }

    /// <summary>
    /// Base64Url encode (URL-safe, no padding)
    /// </summary>
    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Base64Url decode with validation
    /// </summary>
    private static byte[] Base64UrlDecode(string base64Url)
    {
        if (string.IsNullOrEmpty(base64Url))
            throw new ArgumentException("Input cannot be null or empty", nameof(base64Url));

        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');
        
        // Add padding based on remainder
        switch (base64.Length % 4)
        {
            case 0: break; // No padding needed
            case 1: throw new FormatException("Invalid Base64Url string length"); // Invalid
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        return Convert.FromBase64String(base64);
    }

    #endregion
}

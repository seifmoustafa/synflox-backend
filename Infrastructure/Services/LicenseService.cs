using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.Services;
using AutoMapper;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing offline license keys tied to subscriptions
/// Handles key generation, validation, and offline verification
/// 
/// For offline systems, the license key contains the FULL entitlement matrix
/// (unlike online systems which use thin tokens + fetch entitlements)
/// 
/// IMPORTANT: License keys store RAW (unencrypted) IDs because:
/// 1. The entire payload is AES encrypted + HMAC signed
/// 2. We don't need double encryption
/// 3. Encryption key rotation shouldn't invalidate existing licenses
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IModuleRepository _moduleRepo;
    // _entitlementRepo REMOVED - v2.0: Entitlements are now at Plan level
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly LicenseKeySettings _licenseSettings;

    public LicenseService(
        ISubscriptionRepository subscriptionRepo,
        ICompanyRepository companyRepo,
        ISubscriptionPlanRepository planRepo,
        IModuleRepository moduleRepo,
        // entitlementRepo REMOVED - v2.0: Entitlements are now at Plan level
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IOptions<LicenseKeySettings> licenseSettings)
    {
        _subscriptionRepo = subscriptionRepo;
        _companyRepo = companyRepo;
        _planRepo = planRepo;
        _moduleRepo = moduleRepo;
        // _entitlementRepo REMOVED
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _licenseSettings = licenseSettings.Value;
    }

    public async Task<GenerateLicenseKeyResponse> GenerateOfflineLicenseKeyAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, new[] { "Company", "Plan", "Plan.Modules" });
        if (subscription == null)
        {
            throw new NotFoundException(_localizer["Subscription.NotFound"]);
        }

        if (!subscription.IsActive)
        {
            throw new BadRequestException(_localizer["License.CannotGenerateForInactiveSubscription"]);
        }

        // Build entitlement matrix from plan's projects and modules (v2.0: Plan-level entitlements)
        var plan = subscription.Plan;
        var (projects, standaloneModules) = BuildLicenseEntitlementsFromPlan(plan);

        // Create license data payload with FULL entitlements (for offline systems)
        var licenseData = new OfflineLicenseData
        {
            // Identity (RAW IDs - entire payload is encrypted)
            CompanyId = subscription.CompanyId,
            CompanyName = subscription.Company.Name,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan.Name,
            SubscriptionId = subscription.Id,
            
            // Dates
            ExpiryDateUtc = subscription.ExpiryDateUtc,
            IssuedAtUtc = DateTime.UtcNow,
            GraceEndDateUtc = CalculateGraceEndDate(subscription),
            ExportDeadlineUtc = subscription.ExportDeadlineUtc,
            
            // Status
            IsTrial = subscription.IsTrial,
            AccessMode = subscription.AccessMode,
            
            // Versioning
            Version = _licenseSettings.Version,
            EntitlementsVersion = plan.EntitlementVersion,
            
            // Legacy (backward compatibility)
            Features = subscription.Plan.CustomFeatures ?? new List<string>(),
            Modules = subscription.Plan.PlanModules?.Select(m => m.Module.Name).ToList() ?? new List<string>(),
            
            // Enterprise Entitlements (RAW IDs for offline)
            Projects = projects,
            StandaloneModules = standaloneModules
        };

        // Generate encrypted license key
        var licenseKey = GenerateEncryptedLicenseKey(licenseData);

        // Update subscription with license key
        subscription.OfflineLicenseKey = licenseKey;
        subscription.LicenseKeyGeneratedAt = DateTime.UtcNow;
        subscription.LicenseKeyVersion = _licenseSettings.Version;

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        return new GenerateLicenseKeyResponse
        {
            LicenseKey = licenseKey,
            Message = _localizer["License.KeyGeneratedSuccessfully"]
        };
    }

    public async Task<LicenseKeyValidationResponse> ValidateOfflineLicenseKeyAsync(ValidateLicenseKeyRequest request)
    {
        try
        {
            // Decrypt and validate license key
            var licenseData = DecryptAndValidateLicenseKey(request.LicenseKey);
            if (licenseData == null)
            {
                return new LicenseKeyValidationResponse
                {
                    IsValid = false,
                    Message = _localizer["License.InvalidKey"]
                };
            }

            // Check expiry (but allow grace period access)
            var now = DateTime.UtcNow;
            var isExpired = licenseData.ExpiryDateUtc <= now;
            var inGracePeriod = isExpired && licenseData.GraceEndDateUtc.HasValue && licenseData.GraceEndDateUtc > now;
            var inExportOnly = isExpired && !inGracePeriod && licenseData.ExportDeadlineUtc.HasValue && licenseData.ExportDeadlineUtc > now;
            
            // Determine effective access mode
            var effectiveAccessMode = licenseData.AccessMode;
            if (isExpired && !inGracePeriod && !inExportOnly)
            {
                effectiveAccessMode = SubscriptionAccessMode.Blocked;
            }
            else if (inExportOnly)
            {
                effectiveAccessMode = SubscriptionAccessMode.ExportOnly;
            }
            else if (inGracePeriod)
            {
                effectiveAccessMode = SubscriptionAccessMode.GracePeriod;
            }

            // Check if completely blocked
            if (effectiveAccessMode == SubscriptionAccessMode.Blocked)
            {
                return new LicenseKeyValidationResponse
                {
                    IsValid = false,
                    Message = _localizer["License.Expired"],
                    ExpiryDate = licenseData.ExpiryDateUtc,
                    AccessMode = effectiveAccessMode
                };
            }

            // Optional: Check if subscription is still active in database (for online validation)
            // For pure offline systems, skip this check
            if (request.ValidateOnline)
            {
                var subscription = await _subscriptionRepo.GetByIdAsync(licenseData.SubscriptionId, null);
                if (subscription == null || !subscription.IsActive)
                {
                    return new LicenseKeyValidationResponse
                    {
                        IsValid = false,
                        Message = _localizer["License.SubscriptionInactive"]
                    };
                }
                
                // Check if entitlements version has changed (license is stale)
                if ((subscription.Plan?.EntitlementVersion ?? 1) > licenseData.EntitlementsVersion)
                {
                    return new LicenseKeyValidationResponse
                    {
                        IsValid = false,
                        Message = _localizer["License.StaleEntitlements"],
                        EntitlementsVersion = subscription.Plan?.EntitlementVersion ?? 1
                    };
                }
            }

            // Calculate days until expiry
            var daysUntilExpiry = isExpired ? 0 : (int)(licenseData.ExpiryDateUtc - now).TotalDays;

            // Return FULL entitlement matrix for offline systems
            return new LicenseKeyValidationResponse
            {
                IsValid = true,
                Message = _localizer["License.Valid"],
                
                // Identity
                CompanyId = licenseData.CompanyId,
                CompanyName = licenseData.CompanyName,
                PlanName = licenseData.PlanName,
                
                // Dates
                ExpiryDate = licenseData.ExpiryDateUtc,
                GraceEndDate = licenseData.GraceEndDateUtc,
                ExportDeadline = licenseData.ExportDeadlineUtc,
                DaysUntilExpiry = daysUntilExpiry,
                
                // Status
                IsTrial = licenseData.IsTrial,
                AccessMode = effectiveAccessMode,
                
                // Versioning
                KeyVersion = licenseData.Version,
                EntitlementsVersion = licenseData.EntitlementsVersion,
                
                // Legacy
                Features = licenseData.Features,
                Modules = licenseData.Modules,
                
                // Enterprise Entitlements (FULL matrix)
                Projects = licenseData.Projects.Select(p => new LicenseProjectEntitlementDto
                {
                    ProjectId = p.ProjectId,
                    ProjectName = p.ProjectName,
                    ProjectCode = p.ProjectCode,
                    AccessLevel = p.AccessLevel, // enum to enum - direct assignment
                    HasFullAccess = p.HasFullAccess,
                    AllowedOperations = p.AllowedOperations,
                    Modules = p.Modules.Select(m => new LicenseModuleEntitlementDto
                    {
                        ModuleId = m.ModuleId,
                        ModuleName = m.ModuleName,
                        ModuleCode = m.ModuleCode,
                        AccessLevel = m.AccessLevel, // enum to enum - direct assignment
                        AllowedOperations = m.AllowedOperations,
                        AllowedFeatures = m.AllowedFeatures
                    }).ToList()
                }).ToList(),
                
                StandaloneModules = licenseData.StandaloneModules.Select(m => new LicenseModuleEntitlementDto
                {
                    ModuleId = m.ModuleId,
                    ModuleName = m.ModuleName,
                    ModuleCode = m.ModuleCode,
                    AccessLevel = m.AccessLevel, // enum to enum - direct assignment
                    AllowedOperations = m.AllowedOperations,
                    AllowedFeatures = m.AllowedFeatures
                }).ToList()
            };
        }
        catch (Exception)
        {
            return new LicenseKeyValidationResponse
            {
                IsValid = false,
                Message = _localizer["License.ValidationError"]
            };
        }
    }

    public async Task<GenerateLicenseKeyResponse> RegenerateOfflineLicenseKeyAsync(Guid subscriptionId)
    {
        // Same as generate, but for existing subscriptions
        return await GenerateOfflineLicenseKeyAsync(subscriptionId);
    }

    public async Task<IEnumerable<CompanyLicenseKeyDto>> GetCompanyLicenseKeysAsync(Guid companyId)
    {
        var subscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(companyId);
        var result = new List<CompanyLicenseKeyDto>();

        foreach (var subscription in subscriptions)
        {
            var dto = new CompanyLicenseKeyDto
            {
                SubscriptionId = _mapper.Map<string>(subscription.Id), // Encrypted
                PlanId = _mapper.Map<string>(subscription.PlanId), // Encrypted
                PlanName = subscription.Plan?.Name ?? "Unknown Plan",
                GeneratedAt = subscription.LicenseKeyGeneratedAt,
                ExpiryDate = subscription.ExpiryDateUtc,
                IsActive = subscription.IsActive,
                IsExpired = subscription.IsExpired,
                KeyVersion = subscription.LicenseKeyVersion,
                Status = GetLicenseKeyStatus(subscription)
            };

            // Only show license key to SuperAdmin
            if (_currentUserService.AdminTypeName == "SuperAdmin")
            {
                dto.LicenseKey = subscription.OfflineLicenseKey;
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<bool> RevokeLicenseKeyAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null);
        if (subscription == null)
        {
            throw new NotFoundException(_localizer["Subscription.NotFound"]);
        }

        subscription.OfflineLicenseKey = null;
        subscription.LicenseKeyGeneratedAt = null;

        await _subscriptionRepo.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> HasValidLicenseKeyAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId, null);
        return subscription?.OfflineLicenseKey != null && subscription.IsActive && !subscription.IsExpired;
    }

    #region Private Methods

    private string GenerateEncryptedLicenseKey(OfflineLicenseData licenseData)
    {
        // Serialize license data to JSON
        var jsonData = JsonSerializer.Serialize(licenseData);
        var dataBytes = Encoding.UTF8.GetBytes(jsonData);

        // Encrypt the data
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_licenseSettings.EncryptionKey);
        aes.IV = Convert.FromBase64String(_licenseSettings.IV);

        using var encryptor = aes.CreateEncryptor();
        var encryptedData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

        // Create HMAC signature
        using var hmac = new HMACSHA256(Convert.FromBase64String(_licenseSettings.SigningKey));
        var signature = hmac.ComputeHash(encryptedData);

        // Combine encrypted data + signature
        var licenseKeyBytes = new byte[encryptedData.Length + signature.Length];
        Array.Copy(encryptedData, 0, licenseKeyBytes, 0, encryptedData.Length);
        Array.Copy(signature, 0, licenseKeyBytes, encryptedData.Length, signature.Length);

        // Return as base64 string
        return Convert.ToBase64String(licenseKeyBytes);
    }

    private OfflineLicenseData? DecryptAndValidateLicenseKey(string licenseKey)
    {
        try
        {
            var licenseKeyBytes = Convert.FromBase64String(licenseKey);

            // Extract encrypted data and signature
            var signatureLength = 32; // HMAC-SHA256 produces 32 bytes
            if (licenseKeyBytes.Length <= signatureLength)
                return null;

            var encryptedData = new byte[licenseKeyBytes.Length - signatureLength];
            var signature = new byte[signatureLength];

            Array.Copy(licenseKeyBytes, 0, encryptedData, 0, encryptedData.Length);
            Array.Copy(licenseKeyBytes, encryptedData.Length, signature, 0, signatureLength);

            // Verify signature
            using var hmac = new HMACSHA256(Convert.FromBase64String(_licenseSettings.SigningKey));
            var computedSignature = hmac.ComputeHash(encryptedData);

            if (!signature.SequenceEqual(computedSignature))
                return null; // Invalid signature

            // Decrypt data
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_licenseSettings.EncryptionKey);
            aes.IV = Convert.FromBase64String(_licenseSettings.IV);

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            var jsonData = Encoding.UTF8.GetString(decryptedBytes);

            // Deserialize license data
            return JsonSerializer.Deserialize<OfflineLicenseData>(jsonData);
        }
        catch
        {
            return null;
        }
    }

    private static string GetLicenseKeyStatus(Subscription subscription)
    {
        if (string.IsNullOrEmpty(subscription.OfflineLicenseKey))
            return "No Key";

        if (!subscription.IsActive)
            return "Inactive";

        if (subscription.IsExpired)
            return "Expired";

        if (subscription.ExpiryDateUtc <= DateTime.UtcNow.AddDays(30))
            return "Expiring Soon";

        return "Active";
    }

    /// <summary>
    /// Calculate grace period end date based on plan settings
    /// </summary>
    private static DateTime? CalculateGraceEndDate(Subscription subscription)
    {
        // Grace period is calculated from expiry date + plan's grace days
        if (subscription.Plan?.GracePeriodDays > 0)
        {
            return subscription.ExpiryDateUtc.AddDays(subscription.Plan.GracePeriodDays);
        }
        
        // Default: No grace period
        return null;
    }

    /// <summary>
    /// Build license entitlements from Plan's projects and modules (v2.0: Plan-level entitlements)
    /// This uses plan.PlanProjects and plan.PlanModules to determine access
    /// </summary>
    private static (List<LicenseProjectEntitlement> Projects, List<LicenseModuleEntitlement> StandaloneModules) 
        BuildLicenseEntitlementsFromPlan(SubscriptionPlan plan)
    {
        // Build project list from plan's projects
        var projects = (plan.PlanProjects ?? new List<PlanProject>())
            .Where(pp => pp.Project != null)
            .Select(pp => new LicenseProjectEntitlement
            {
                ProjectId = pp.ProjectId,
                ProjectName = pp.Project?.Name ?? string.Empty,
                ProjectCode = pp.Project?.Name?.Replace(" ", "").ToUpperInvariant() ?? string.Empty,
                AccessLevel = EntitlementAccessLevel.Full, // TODO: Get from PlanEntitlement when created
                HasFullAccess = true,
                AllowedOperations = new List<string> { "Create", "Read", "Update", "Delete", "Export" },
                Modules = (pp.Project?.ProjectModules ?? new List<ProjectModule>())
                    .Where(pm => pm.Module != null)
                    .Select(pm => new LicenseModuleEntitlement
                    {
                        ModuleId = pm.ModuleId,
                        ModuleName = pm.Module?.Name ?? string.Empty,
                        ModuleCode = pm.Module?.Name?.Replace(" ", "").ToUpperInvariant() ?? string.Empty,
                        AccessLevel = EntitlementAccessLevel.Full,
                        AllowedOperations = new List<string> { "Create", "Read", "Update", "Delete", "Export" },
                        AllowedFeatures = new List<string>()
                    }).ToList()
            }).ToList();

        // Build standalone modules from plan's direct modules (not part of a project)
        var standaloneModules = (plan.PlanModules ?? new List<PlanModule>())
            .Where(pm => pm.Module != null)
            .Select(pm => new LicenseModuleEntitlement
            {
                ModuleId = pm.ModuleId,
                ModuleName = pm.Module?.Name ?? string.Empty,
                ModuleCode = pm.Module?.Name?.Replace(" ", "").ToUpperInvariant() ?? string.Empty,
                AccessLevel = EntitlementAccessLevel.Full, // TODO: Get from PlanEntitlement when created
                AllowedOperations = new List<string> { "Create", "Read", "Update", "Delete", "Export" },
                AllowedFeatures = new List<string>()
            }).ToList();

        return (projects, standaloneModules);
    }

    /// <summary>
    /// Parse comma-separated features string to list
    /// </summary>
    private static List<string> ParseFeatures(string? features)
    {
        if (string.IsNullOrWhiteSpace(features))
            return new List<string>();
        
        return features.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    #endregion
}

/// <summary>
/// Internal class for license key data structure
/// Contains FULL entitlement matrix for offline systems
/// </summary>
internal class OfflineLicenseData
{
    // ========== IDENTITY ==========
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    
    // ========== DATES ==========
    public DateTime ExpiryDateUtc { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? GraceEndDateUtc { get; set; }
    public DateTime? ExportDeadlineUtc { get; set; }
    
    // ========== STATUS ==========
    public bool IsTrial { get; set; }
    public SubscriptionAccessMode AccessMode { get; set; } = SubscriptionAccessMode.Full;
    
    // ========== VERSIONING ==========
    public int Version { get; set; }
    public int EntitlementsVersion { get; set; }
    
    // ========== LEGACY (for backward compatibility) ==========
    public List<string> Features { get; set; } = new();
    public List<string> Modules { get; set; } = new();
    
    // ========== ENTERPRISE ENTITLEMENTS ==========
    /// <summary>
    /// Full project-level entitlements with modules
    /// </summary>
    public List<LicenseProjectEntitlement> Projects { get; set; } = new();
    
    /// <summary>
    /// Standalone module entitlements (not part of a project)
    /// </summary>
    public List<LicenseModuleEntitlement> StandaloneModules { get; set; } = new();
}

/// <summary>
/// Project entitlement in license key
/// </summary>
internal class LicenseProjectEntitlement
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;
    public bool HasFullAccess { get; set; }
    public List<string> AllowedOperations { get; set; } = new();
    public List<LicenseModuleEntitlement> Modules { get; set; } = new();
}

/// <summary>
/// Module entitlement in license key
/// </summary>
internal class LicenseModuleEntitlement
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;
    public List<string> AllowedOperations { get; set; } = new();
    public List<string> AllowedFeatures { get; set; } = new();
}

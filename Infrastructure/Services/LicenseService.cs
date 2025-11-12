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
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing offline license keys tied to subscriptions
/// Handles key generation, validation, and offline verification
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IModuleRepository _moduleRepo;
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

        // Create license data payload
        var licenseData = new OfflineLicenseData
        {
            CompanyId = subscription.CompanyId,
            CompanyName = subscription.Company.Name,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan.Name,
            SubscriptionId = subscription.Id,
            ExpiryDateUtc = subscription.ExpiryDateUtc,
            IssuedAtUtc = DateTime.UtcNow,
            IsTrial = subscription.IsTrial,
            Features = subscription.Plan.CustomFeatures ?? new List<string>(),
            Modules = subscription.Plan.PlanModules?.Select(m => m.Module.Name).ToList() ?? new List<string>(),
            Version = _licenseSettings.Version
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

            // Check expiry
            if (licenseData.ExpiryDateUtc <= DateTime.UtcNow)
            {
                return new LicenseKeyValidationResponse
                {
                    IsValid = false,
                    Message = _localizer["License.Expired"],
                    ExpiryDate = licenseData.ExpiryDateUtc
                };
            }

            // Check if subscription is still active in database
            var subscription = await _subscriptionRepo.GetByIdAsync(licenseData.SubscriptionId, null);
            if (subscription == null || !subscription.IsActive)
            {
                return new LicenseKeyValidationResponse
                {
                    IsValid = false,
                    Message = _localizer["License.SubscriptionInactive"]
                };
            }

            // Calculate days until expiry
            var daysUntilExpiry = (int)(licenseData.ExpiryDateUtc - DateTime.UtcNow).TotalDays;

            return new LicenseKeyValidationResponse
            {
                IsValid = true,
                Message = _localizer["License.Valid"],
                ExpiryDate = licenseData.ExpiryDateUtc,
                CompanyName = licenseData.CompanyName,
                PlanName = licenseData.PlanName,
                Features = licenseData.Features,
                Modules = licenseData.Modules,
                IsTrial = licenseData.IsTrial,
                DaysUntilExpiry = daysUntilExpiry > 0 ? daysUntilExpiry : 0,
                KeyVersion = licenseData.Version
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

    #endregion
}

/// <summary>
/// Internal class for license key data structure
/// </summary>
internal class OfflineLicenseData
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public bool IsTrial { get; set; }
    public List<string> Features { get; set; } = new();
    public List<string> Modules { get; set; } = new();
    public int Version { get; set; }
}

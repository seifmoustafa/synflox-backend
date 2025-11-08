using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    public class LicensingService : ILicensingService
    {
        private readonly ICompanyRepository _repository;
        private readonly ICompanyService _companyService;
        private readonly IMapper _mapper;
        private readonly ILocalizationService _localizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly LicenseKeySettings _licenseKeySettings;
        private readonly IIdEncryptionService _idEncryption;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISubscriptionHistoryService _historyService;
        private readonly INotificationService _notificationService;
        private readonly IWebhookService _webhookService;

        public LicensingService(
            ICompanyRepository repository,
            ICompanyService companyService,
            IMapper mapper,
            ILocalizationService localizer,
            IUnitOfWork unitOfWork,
            IOptions<LicenseKeySettings> licenseKeySettings,
            IIdEncryptionService idEncryption,
            ICurrentUserService currentUserService,
            ISubscriptionHistoryService historyService,
            INotificationService notificationService,
            IWebhookService webhookService)
        {
            _repository = repository;
            _companyService = companyService;
            _mapper = mapper;
            _localizer = localizer;
            _unitOfWork = unitOfWork;
            _licenseKeySettings = licenseKeySettings.Value;
            _idEncryption = idEncryption;
            _currentUserService = currentUserService;
            _historyService = historyService;
            _notificationService = notificationService;
            _webhookService = webhookService;
        }

        public async Task<CompanyDto> ActivateCompanyAsync(Guid id, DateTime expiryDate)
        {
            if (expiryDate <= DateTime.UtcNow)
            {
                throw new BadRequestException(_localizer["Licensing.InvalidExpiryDate"]);
            }

            var company = await _repository.GetByIdAsync(id, null);
            if (company == null)
            {
                throw new NotFoundException(_localizer["Licensing.CompanyNotFound"]);
            }

            if (company.IsActive && company.ExpiryDate >= DateTime.UtcNow)
            {
                throw new BadRequestException(_localizer["Licensing.CompanyAlreadyActive"]);
            }

            var oldExpiryDate = company.ExpiryDate;
            var oldIsActive = company.IsActive;

            company.IsActive = true;
            company.ExpiryDate = expiryDate;
            company.IsExpired = false; // Reset expired flag when activating
            await _repository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();

            // Log to history
            await _historyService.LogSubscriptionEventAsync(
                company.Id,
                Domain.Enums.SubscriptionHistoryActionType.Activated,
                new { IsActive = oldIsActive, ExpiryDate = oldExpiryDate },
                new { IsActive = company.IsActive, ExpiryDate = company.ExpiryDate },
                _currentUserService.UserId,
                $"Subscription activated with expiry date: {expiryDate:yyyy-MM-dd}");

            // Create notification
            try
            {
                var title = _localizer["Notification.ActivatedTitle"];
                var message = string.Format(_localizer["Notification.ActivatedMessage"], company.Name, expiryDate.ToString("yyyy-MM-dd"));
                await _notificationService.CreateNotificationAsync(
                    company.Id,
                    Domain.Enums.NotificationType.Activated,
                    title,
                    message);
            }
            catch (Exception notifEx)
            {
                // Log but don't fail the operation
            }

            // Trigger webhook
            // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
            // This is an exception to the rule - encryption here is acceptable for external API contracts
            try
            {
                var webhookPayload = new { CompanyId = _idEncryption.Encrypt(company.Id), CompanyName = company.Name, ExpiryDate = expiryDate, EventType = "CompanyActivated" };
                await _webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanyActivated, webhookPayload);
            }
            catch (Exception webhookEx)
            {
                // Log but don't fail the operation
            }

            var dto = _mapper.Map<CompanyDto>(company);
            SetLicenseKeyIfSuperAdmin(dto, company);
            return dto;
        }

        public async Task<CompanyDto> SuspendCompanyAsync(Guid id)
        {
            var company = await _repository.GetByIdAsync(id, null);
            if (company == null)
            {
                throw new NotFoundException(_localizer["Licensing.CompanyNotFound"]);
            }

            if (!company.IsActive)
            {
                throw new BadRequestException(_localizer["Licensing.CompanyAlreadySuspended"]);
            }

            var oldIsActive = company.IsActive;

            company.IsActive = false;
            await _repository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();

            // Log to history
            await _historyService.LogSubscriptionEventAsync(
                company.Id,
                Domain.Enums.SubscriptionHistoryActionType.Suspended,
                new { IsActive = oldIsActive },
                new { IsActive = company.IsActive },
                _currentUserService.UserId,
                "Subscription suspended");

            // Create notification
            try
            {
                var title = _localizer["Notification.SuspendedTitle"];
                var message = string.Format(_localizer["Notification.SuspendedMessage"], company.Name);
                await _notificationService.CreateNotificationAsync(
                    company.Id,
                    Domain.Enums.NotificationType.Suspended,
                    title,
                    message);
            }
            catch (Exception notifEx)
            {
                // Log but don't fail the operation
            }

            // Trigger webhook
            // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
            // This is an exception to the rule - encryption here is acceptable for external API contracts
            try
            {
                var webhookPayload = new { CompanyId = _idEncryption.Encrypt(company.Id), CompanyName = company.Name, EventType = "CompanySuspended" };
                await _webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanySuspended, webhookPayload);
            }
            catch (Exception webhookEx)
            {
                // Log but don't fail the operation
            }

            var dto = _mapper.Map<CompanyDto>(company);
            SetLicenseKeyIfSuperAdmin(dto, company);
            return dto;
        }

        public async Task<CompanyDto> ResumeCompanyAsync(Guid id)
        {
            var company = await _repository.GetByIdAsync(id, null);
            if (company == null)
            {
                throw new NotFoundException(_localizer["Licensing.CompanyNotFound"]);
            }

            if (company.IsActive)
            {
                throw new BadRequestException(_localizer["Licensing.CompanyAlreadyActive"]);
            }

            var oldIsActive = company.IsActive;

            company.IsActive = true;
            await _repository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();

            // Log to history
            await _historyService.LogSubscriptionEventAsync(
                company.Id,
                Domain.Enums.SubscriptionHistoryActionType.Resumed,
                new { IsActive = oldIsActive },
                new { IsActive = company.IsActive },
                _currentUserService.UserId,
                "Subscription resumed");

            // Create notification
            try
            {
                var title = _localizer["Notification.ResumedTitle"];
                var message = string.Format(_localizer["Notification.ResumedMessage"], company.Name);
                await _notificationService.CreateNotificationAsync(
                    company.Id,
                    Domain.Enums.NotificationType.Resumed,
                    title,
                    message);
            }
            catch (Exception notifEx)
            {
                // Log but don't fail the operation
            }

            // Trigger webhook
            // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
            // This is an exception to the rule - encryption here is acceptable for external API contracts
            try
            {
                var webhookPayload = new { CompanyId = _idEncryption.Encrypt(company.Id), CompanyName = company.Name, EventType = "CompanyResumed" };
                await _webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanyResumed, webhookPayload);
            }
            catch (Exception webhookEx)
            {
                // Log but don't fail the operation
            }

            var dto = _mapper.Map<CompanyDto>(company);
            SetLicenseKeyIfSuperAdmin(dto, company);
            return dto;
        }

        public async Task<CompanyDto> ExtendCompanyAsync(Guid id, DateTime newExpiryDate)
        {
            if (newExpiryDate <= DateTime.UtcNow)
            {
                throw new BadRequestException(_localizer["Licensing.InvalidExpiryDate"]);
            }

            var company = await _repository.GetByIdAsync(id, null);
            if (company == null)
            {
                throw new NotFoundException(_localizer["Licensing.CompanyNotFound"]);
            }

            var oldExpiryDate = company.ExpiryDate;

            company.ExpiryDate = newExpiryDate;
            company.IsExpired = false; // Reset expired flag when extending
            await _repository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();

            // Log to history
            await _historyService.LogSubscriptionEventAsync(
                company.Id,
                Domain.Enums.SubscriptionHistoryActionType.Extended,
                new { ExpiryDate = oldExpiryDate },
                new { ExpiryDate = company.ExpiryDate },
                _currentUserService.UserId,
                $"Subscription extended to: {newExpiryDate:yyyy-MM-dd}");

            // Create notification
            try
            {
                var title = _localizer["Notification.ExtendedTitle"];
                var message = string.Format(_localizer["Notification.ExtendedMessage"], company.Name, newExpiryDate.ToString("yyyy-MM-dd"));
                await _notificationService.CreateNotificationAsync(
                    company.Id,
                    Domain.Enums.NotificationType.Extended,
                    title,
                    message);
            }
            catch (Exception notifEx)
            {
                // Log but don't fail the operation
            }

            // Trigger webhook
            // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
            // This is an exception to the rule - encryption here is acceptable for external API contracts
            try
            {
                var webhookPayload = new { CompanyId = _idEncryption.Encrypt(company.Id), CompanyName = company.Name, NewExpiryDate = newExpiryDate, EventType = "CompanyExtended" };
                await _webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanyExtended, webhookPayload);
            }
            catch (Exception webhookEx)
            {
                // Log but don't fail the operation
            }

            var dto = _mapper.Map<CompanyDto>(company);
            SetLicenseKeyIfSuperAdmin(dto, company);
            return dto;
        }

        public async Task<CompanyStatusResponse> CheckCompanyStatusAsync(Guid id)
        {
            // Get company entity for status calculation (need entity, not DTO)
            var company = await _repository.GetByIdAsync(id, null);
            if (company == null)
            {
                throw new NotFoundException(_localizer["Licensing.CompanyNotFound"]);
            }

            var status = CalculateStatus(company);
            var statusMessage = GetStatusMessage(status, _localizer);

            return new CompanyStatusResponse
            {
                Status = status,
                ExpiryDate = company.ExpiryDate,
                IsActive = company.IsActive,
                StatusMessage = statusMessage
            };
        }

        public async Task<string> GenerateLicenseKeyAsync(Guid companyId)
        {
            var company = await _repository.GetByIdAsync(companyId, null);
            if (company == null)
            {
                throw new NotFoundException(_localizer["Licensing.CompanyNotFound"]);
            }

            var licenseKey = GenerateLicenseKey(company);
            company.LicenseKey = licenseKey;
            await _repository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();

            return licenseKey;
        }

        public async Task<string> RegenerateLicenseKeyAsync(Guid companyId)
        {
            var company = await _repository.GetByIdAsync(companyId, null);
            if (company == null)
            {
                throw new NotFoundException(_localizer["Licensing.CompanyNotFound"]);
            }

            var licenseKey = GenerateLicenseKey(company);
            company.LicenseKey = licenseKey;
            await _repository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();

            return licenseKey;
        }

        public async Task<LicenseKeyValidationResponse> ValidateLicenseKeyAsync(string licenseKey)
        {
            try
            {
                // Decrypt and validate the license key
                var payload = DecryptAndValidateLicenseKey(licenseKey);
                if (payload == null)
                {
                    return new LicenseKeyValidationResponse
                    {
                        IsValid = false,
                        Status = LicenseStatus.Expired,
                        Message = _localizer["Licensing.LicenseKeyInvalid"]
                    };
                }

                // Get the company
                var company = await _repository.GetByIdAsync(payload.CompanyId, null);
                if (company == null || company.LicenseKey != licenseKey)
                {
                    return new LicenseKeyValidationResponse
                    {
                        IsValid = false,
                        Status = LicenseStatus.Expired,
                        Message = _localizer["Licensing.LicenseKeyInvalid"]
                    };
                }

                // Check for clock tampering
                var currentTime = DateTime.UtcNow;
                var clockTampered = currentTime < payload.IssuedDate;

                // Calculate status
                var status = CalculateStatus(company);

                return new LicenseKeyValidationResponse
                {
                    IsValid = status == LicenseStatus.Active && !clockTampered,
                    Status = status,
                    ExpiryDate = company.ExpiryDate,
                    IsActive = company.IsActive,
                    ClockTampered = clockTampered,
                    // Encrypt ID for response DTO (Entity → DTO encryption handled by mapper)
                    // This is manual encryption for response DTOs that don't go through mapper
                    CompanyId = _idEncryption.Encrypt(company.Id),
                    Message = clockTampered 
                        ? _localizer["Licensing.SystemClockTampered"]
                        : GetStatusMessage(status, _localizer)
                };
            }
            catch
            {
                return new LicenseKeyValidationResponse
                {
                    IsValid = false,
                    Status = LicenseStatus.Expired,
                    Message = _localizer["Licensing.LicenseKeyInvalid"]
                };
            }
        }

        private LicenseStatus CalculateStatus(Company company)
        {
            var now = DateTime.UtcNow;

            // Check trial expiry first
            if (company.IsTrial && company.TrialEndDate.HasValue && company.TrialEndDate.Value < now)
            {
                return LicenseStatus.Expired;
            }

            // Expired takes precedence - check both IsExpired flag and ExpiryDate
            if (company.IsExpired || (company.ExpiryDate.HasValue && company.ExpiryDate.Value < now))
            {
                return LicenseStatus.Expired;
            }

            // If not active, it's suspended
            if (!company.IsActive)
            {
                return LicenseStatus.Suspended;
            }

            // If it's a trial, return Active (trial is considered active until expiry)
            if (company.IsTrial)
            {
                return LicenseStatus.Active;
            }

            // Otherwise, it's active
            return LicenseStatus.Active;
        }

        private string GetStatusMessage(LicenseStatus status, ILocalizationService localizer)
        {
            return status switch
            {
                LicenseStatus.Active => localizer["Licensing.SubscriptionActive"],
                LicenseStatus.Expired => localizer["Licensing.SubscriptionExpired"],
                LicenseStatus.Suspended => localizer["Licensing.SubscriptionSuspended"],
                _ => string.Empty
            };
        }

        private string GenerateLicenseKey(Company company)
        {
            var now = DateTime.UtcNow;
            var payload = new LicenseKeyPayload
            {
                CompanyId = company.Id,
                ExpiryDate = company.ExpiryDate ?? DateTime.MaxValue,
                IssuedDate = now,
                Version = _licenseKeySettings.Version
            };

            // Serialize payload to JSON
            var jsonPayload = JsonSerializer.Serialize(payload);
            var payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

            // Sign the payload with HMAC SHA256
            var signingKeyBytes = Convert.FromBase64String(_licenseKeySettings.SigningKey);
            using var hmac = new HMACSHA256(signingKeyBytes);
            var signature = hmac.ComputeHash(payloadBytes);
            var signedPayload = new SignedPayload
            {
                Payload = jsonPayload,
                Signature = Convert.ToBase64String(signature)
            };

            var signedJson = JsonSerializer.Serialize(signedPayload);
            var signedBytes = Encoding.UTF8.GetBytes(signedJson);

            // Encrypt with AES-256
            var encryptionKeyBytes = Convert.FromBase64String(_licenseKeySettings.EncryptionKey);
            var ivBytes = Convert.FromBase64String(_licenseKeySettings.IV);

            using var aes = Aes.Create();
            aes.Key = encryptionKeyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var encryptedBytes = encryptor.TransformFinalBlock(signedBytes, 0, signedBytes.Length);

            // Return base64 encoded encrypted license key
            return Convert.ToBase64String(encryptedBytes);
        }

        private LicenseKeyPayload? DecryptAndValidateLicenseKey(string licenseKey)
        {
            try
            {
                // Decode from base64
                var encryptedBytes = Convert.FromBase64String(licenseKey);

                // Decrypt with AES-256
                var encryptionKeyBytes = Convert.FromBase64String(_licenseKeySettings.EncryptionKey);
                var ivBytes = Convert.FromBase64String(_licenseKeySettings.IV);

                using var aes = Aes.Create();
                aes.Key = encryptionKeyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                var signedJson = Encoding.UTF8.GetString(decryptedBytes);

                // Deserialize signed payload
                var signedPayload = JsonSerializer.Deserialize<SignedPayload>(signedJson);
                if (signedPayload == null) return null;

                // Verify signature
                var payloadBytes = Encoding.UTF8.GetBytes(signedPayload.Payload);
                var signingKeyBytes = Convert.FromBase64String(_licenseKeySettings.SigningKey);
                using var hmac = new HMACSHA256(signingKeyBytes);
                var computedSignature = hmac.ComputeHash(payloadBytes);
                var providedSignature = Convert.FromBase64String(signedPayload.Signature);

                if (!computedSignature.SequenceEqual(providedSignature))
                {
                    return null; // Signature verification failed
                }

                // Deserialize and return payload
                return JsonSerializer.Deserialize<LicenseKeyPayload>(signedPayload.Payload);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sets LicenseKey in the DTO only if the current user is SuperAdmin.
        /// </summary>
        private void SetLicenseKeyIfSuperAdmin(CompanyDto dto, Company company)
        {
            if (_currentUserService.AdminTypeName == "SuperAdmin")
            {
                dto.LicenseKey = company.LicenseKey;
            }
        }

        private class LicenseKeyPayload
        {
            public Guid CompanyId { get; set; }
            public DateTime ExpiryDate { get; set; }
            public DateTime IssuedDate { get; set; }
            public int Version { get; set; }
        }

        private class SignedPayload
        {
            public string Payload { get; set; } = string.Empty;
            public string Signature { get; set; } = string.Empty;
        }

        public async Task<BulkOperationResponse> BulkActivateAsync(List<Guid> companyIds, DateTime expiryDate)
        {
            var response = new BulkOperationResponse
            {
                TotalRequested = companyIds.Count
            };

            foreach (var companyId in companyIds)
            {
                try
                {
                    await ActivateCompanyAsync(companyId, expiryDate);
                    var company = await _repository.GetByIdAsync(companyId, null);
                    response.Results.Add(new BulkOperationResult
                    {
                        // Encrypt ID for response DTO (Entity → DTO encryption handled by mapper)
                        // This is manual encryption for response DTOs that don't go through mapper
                        CompanyId = _idEncryption.Encrypt(companyId),
                        CompanyName = company?.Name ?? "Unknown",
                        Success = true
                    });
                    response.Successful++;
                }
                catch (Exception ex)
                {
                    var company = await _repository.GetByIdAsync(companyId, null);
                    response.Results.Add(new BulkOperationResult
                    {
                        // Encrypt ID for response DTO (Entity → DTO encryption handled by mapper)
                        // This is manual encryption for response DTOs that don't go through mapper
                        CompanyId = _idEncryption.Encrypt(companyId),
                        CompanyName = company?.Name ?? "Unknown",
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                    response.Failed++;
                }
            }

            return response;
        }

        public async Task<BulkOperationResponse> BulkSuspendAsync(List<Guid> companyIds)
        {
            var response = new BulkOperationResponse
            {
                TotalRequested = companyIds.Count
            };

            foreach (var companyId in companyIds)
            {
                try
                {
                    await SuspendCompanyAsync(companyId);
                    var company = await _repository.GetByIdAsync(companyId, null);
                    response.Results.Add(new BulkOperationResult
                    {
                        CompanyId = _idEncryption.Encrypt(companyId),
                        CompanyName = company?.Name ?? "Unknown",
                        Success = true
                    });
                    response.Successful++;
                }
                catch (Exception ex)
                {
                    var company = await _repository.GetByIdAsync(companyId, null);
                    response.Results.Add(new BulkOperationResult
                    {
                        // Encrypt ID for response DTO (Entity → DTO encryption handled by mapper)
                        // This is manual encryption for response DTOs that don't go through mapper
                        CompanyId = _idEncryption.Encrypt(companyId),
                        CompanyName = company?.Name ?? "Unknown",
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                    response.Failed++;
                }
            }

            return response;
        }

        public async Task<BulkOperationResponse> BulkResumeAsync(List<Guid> companyIds)
        {
            var response = new BulkOperationResponse
            {
                TotalRequested = companyIds.Count
            };

            foreach (var companyId in companyIds)
            {
                try
                {
                    await ResumeCompanyAsync(companyId);
                    var company = await _repository.GetByIdAsync(companyId, null);
                    response.Results.Add(new BulkOperationResult
                    {
                        CompanyId = _idEncryption.Encrypt(companyId),
                        CompanyName = company?.Name ?? "Unknown",
                        Success = true
                    });
                    response.Successful++;
                }
                catch (Exception ex)
                {
                    var company = await _repository.GetByIdAsync(companyId, null);
                    response.Results.Add(new BulkOperationResult
                    {
                        // Encrypt ID for response DTO (Entity → DTO encryption handled by mapper)
                        // This is manual encryption for response DTOs that don't go through mapper
                        CompanyId = _idEncryption.Encrypt(companyId),
                        CompanyName = company?.Name ?? "Unknown",
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                    response.Failed++;
                }
            }

            return response;
        }

        public async Task<BulkOperationResponse> BulkExtendAsync(List<Guid> companyIds, DateTime newExpiryDate)
        {
            var response = new BulkOperationResponse
            {
                TotalRequested = companyIds.Count
            };

            foreach (var companyId in companyIds)
            {
                try
                {
                    await ExtendCompanyAsync(companyId, newExpiryDate);
                    var company = await _repository.GetByIdAsync(companyId, null);
                    response.Results.Add(new BulkOperationResult
                    {
                        CompanyId = _idEncryption.Encrypt(companyId),
                        CompanyName = company?.Name ?? "Unknown",
                        Success = true
                    });
                    response.Successful++;
                }
                catch (Exception ex)
                {
                    var company = await _repository.GetByIdAsync(companyId, null);
                    response.Results.Add(new BulkOperationResult
                    {
                        // Encrypt ID for response DTO (Entity → DTO encryption handled by mapper)
                        // This is manual encryption for response DTOs that don't go through mapper
                        CompanyId = _idEncryption.Encrypt(companyId),
                        CompanyName = company?.Name ?? "Unknown",
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                    response.Failed++;
                }
            }

            return response;
        }

        public async Task<CompanyDto> StartTrialAsync(Guid companyId, int trialDays)
        {
            var company = await _repository.GetByIdAsync(companyId, null);
            if (company == null || company.IsDeleted)
            {
                throw new NotFoundException(_localizer["Company.CompanyNotFound"]);
            }

            if (company.IsTrial && company.TrialEndDate.HasValue && company.TrialEndDate.Value > DateTime.UtcNow)
            {
                throw new BadRequestException(_localizer["Trial.AlreadyActive"]);
            }

            company.IsTrial = true;
            company.TrialEndDate = DateTime.UtcNow.AddDays(trialDays);
            company.IsActive = true;
            company.IsExpired = false;
            company.UpdatedTimestamp = DateTime.UtcNow;

            await _repository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();

            // Log to history
            await _historyService.LogSubscriptionEventAsync(
                company.Id,
                Domain.Enums.SubscriptionHistoryActionType.Activated,
                new { IsTrial = false, TrialEndDate = (DateTime?)null },
                new { IsTrial = company.IsTrial, TrialEndDate = company.TrialEndDate },
                _currentUserService.UserId,
                $"Trial started for {trialDays} days");

            // Create notification
            try
            {
                var title = _localizer["Notification.TrialStartedTitle"];
                var message = string.Format(_localizer["Notification.TrialStartedMessage"], company.Name, trialDays, company.TrialEndDate.Value.ToString("yyyy-MM-dd"));
                await _notificationService.CreateNotificationAsync(
                    company.Id,
                    Domain.Enums.NotificationType.Activated,
                    title,
                    message);
            }
            catch (Exception notifEx)
            {
                // Log but don't fail the operation
            }

            // Trigger webhook
            // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
            // This is an exception to the rule - encryption here is acceptable for external API contracts
            try
            {
                var webhookPayload = new { CompanyId = _idEncryption.Encrypt(company.Id), CompanyName = company.Name, TrialDays = trialDays, TrialEndDate = company.TrialEndDate.Value.ToString("yyyy-MM-dd"), EventType = "TrialStarted" };
                await _webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanyActivated, webhookPayload);
            }
            catch (Exception webhookEx)
            {
                // Log but don't fail the operation
            }

            var dto = _mapper.Map<CompanyDto>(company);
            SetLicenseKeyIfSuperAdmin(dto, company);
            return dto;
        }

        public async Task<CompanyDto> ConvertTrialToActiveAsync(Guid companyId, DateTime expiryDate)
        {
            var company = await _repository.GetByIdAsync(companyId, null);
            if (company == null || company.IsDeleted)
            {
                throw new NotFoundException(_localizer["Company.CompanyNotFound"]);
            }

            if (!company.IsTrial)
            {
                throw new BadRequestException(_localizer["Trial.NotTrial"]);
            }

            var oldIsTrial = company.IsTrial;
            var oldTrialEndDate = company.TrialEndDate;

            company.IsTrial = false;
            company.TrialEndDate = null;
            company.ExpiryDate = expiryDate;
            company.IsActive = true;
            company.IsExpired = false;
            company.UpdatedTimestamp = DateTime.UtcNow;

            await _repository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();

            // Log to history
            await _historyService.LogSubscriptionEventAsync(
                company.Id,
                Domain.Enums.SubscriptionHistoryActionType.Activated,
                new { IsTrial = oldIsTrial, TrialEndDate = oldTrialEndDate, ExpiryDate = (DateTime?)null },
                new { IsTrial = company.IsTrial, TrialEndDate = company.TrialEndDate, ExpiryDate = company.ExpiryDate },
                _currentUserService.UserId,
                $"Trial converted to active subscription with expiry date: {expiryDate:yyyy-MM-dd}");

            // Create notification
            try
            {
                var title = _localizer["Notification.TrialConvertedTitle"];
                var message = string.Format(_localizer["Notification.TrialConvertedMessage"], company.Name, expiryDate.ToString("yyyy-MM-dd"));
                await _notificationService.CreateNotificationAsync(
                    company.Id,
                    Domain.Enums.NotificationType.Activated,
                    title,
                    message);
            }
            catch (Exception notifEx)
            {
                // Log but don't fail the operation
            }

            // Trigger webhook
            // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
            // This is an exception to the rule - encryption here is acceptable for external API contracts
            try
            {
                var webhookPayload = new { CompanyId = _idEncryption.Encrypt(company.Id), CompanyName = company.Name, ExpiryDate = expiryDate, EventType = "TrialConverted" };
                await _webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanyActivated, webhookPayload);
            }
            catch (Exception webhookEx)
            {
                // Log but don't fail the operation
            }

            var dto = _mapper.Map<CompanyDto>(company);
            SetLicenseKeyIfSuperAdmin(dto, company);
            return dto;
        }
    }
}


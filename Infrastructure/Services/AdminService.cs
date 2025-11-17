using Application.DTOs.Admin;
using Application.Services;
using AutoMapper;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OtpNet;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;

namespace Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyRepository _companyRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    
    public AdminService(
        IAdminRepository repo, 
        IPasswordHasher hasher, 
        IMapper mapper,
        ILocalizationService localizer, 
        IUnitOfWork unitOfWork,
        ICompanyRepository companyRepo,
        ISubscriptionRepository subscriptionRepo)
    {
        _repo = repo;
        _hasher = hasher;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _companyRepo = companyRepo;
        _subscriptionRepo = subscriptionRepo;
    }

    public async Task<(IEnumerable<AdminDto> Admins, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search)
    {
        var (entities, meta) = await _repo.GetAllAsync(
            ["AdminType"],
            page,
            pageSize,
            search);
        var dtos = _mapper.Map<IEnumerable<AdminDto>>(entities);
        return (dtos, meta);
    }

    public async Task<AdminDto?> GetByIdAsync(GetAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var admin = await _repo.GetByIdAsync(decryptedId, ["AdminType"]);
        if (admin is null) return null;
        return _mapper.Map<AdminDto>(admin);
    }

    /// <summary>
    /// Direct overload for internal calls (from JWT) - accepts decrypted Guid
    /// NO DECRYPTION NEEDED - ID is already decrypted from JWT
    /// </summary>
    public async Task<AdminDto?> GetByIdAsync(Guid id)
    {
        var admin = await _repo.GetByIdAsync(id, ["AdminType"]);
        if (admin is null) return null;
        return _mapper.Map<AdminDto>(admin);
    }

    public async Task<AdminDto> CreateAsync(CreateAdminDto dto)
    {
        var admin = _mapper.Map<Admin>(dto);

        var existing = await _repo.GetByUserNameAsync(admin.Username);
        if (existing != null)
            throw new BadRequestException(_localizer["UsernameTaken"]);

        admin.Password = _hasher.HashPassword(admin.Password);
        var created = await _repo.AddAsync(admin);
        await _unitOfWork.SaveChangesAsync();
        var withType = await _repo.GetByIdAsync(created.Id, ["AdminType"]);
        return _mapper.Map<AdminDto>(withType);
    }

    public async Task<AdminDto?> UpdateAsync(UpdateAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var admin = await _repo.GetByIdAsync(decryptedId, ["AdminType"]);
        if (admin is null) return null;

        if (!string.IsNullOrEmpty(request.UpdateData.Username) && request.UpdateData.Username != admin.Username)
        {
            var existing = await _repo.GetByUserNameAsync(request.UpdateData.Username);
            if (existing != null && existing.Id != decryptedId)
                throw new BadRequestException(_localizer["UsernameTaken"]);
        }

        _mapper.Map(request.UpdateData, admin);
        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
        admin = await _repo.GetByIdAsync(decryptedId, ["AdminType"]);
        return _mapper.Map<AdminDto>(admin);
    }

    /// <summary>
    /// Direct overload for internal calls (from JWT) - accepts decrypted Guid
    /// NO DECRYPTION NEEDED - ID is already decrypted from JWT
    /// </summary>
    public async Task<AdminDto?> UpdateAsync(Guid id, UpdateAdminRequest data)
    {
        var entity = await _repo.GetByIdAsync(id, ["AdminType"]);
        if (entity is null) return null;

        if (!string.IsNullOrEmpty(data.Username) && data.Username != entity.Username)
        {
            var existing = await _repo.GetByUserNameAsync(data.Username);
            if (existing != null && existing.Id != id)
                throw new BadRequestException(_localizer["UsernameTaken"]);
        }

        _mapper.Map(data, entity);
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        entity = await _repo.GetByIdAsync(id, ["AdminType"]);
        return _mapper.Map<AdminDto>(entity);
    }

    public async Task<bool> DeleteAsync(GetAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        await _repo.DeleteAsync(decryptedId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword)
    {
        var admin = await _repo.GetByIdAsync(id, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);
        bool valid = _hasher.VerifyPassword(currentPassword, admin.Password);
        if (!valid)
            throw new BadRequestException(_localizer["InvalidCurrentPassword"]);

        admin.Password = _hasher.HashPassword(newPassword);
        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(ChangePasswordByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var admin = await _repo.GetByIdAsync(decryptedId, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);
        admin.Password = _hasher.HashPassword(request.NewPassword);
        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> ActivateAsync(GetAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var admin = await _repo.GetByIdAsync(decryptedId, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);
        if (!admin.IsActive)
        {
            admin.IsActive = true;
            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> DeactivateAsync(GetAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var admin = await _repo.GetByIdAsync(decryptedId, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);
        if (admin.IsActive)
        {
            admin.IsActive = false;
            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> ActivateSelectedAsync(AdminIdsRequest request)
    {
        // Use AutoMapper to decrypt the IDs
        var decryptedIds = _mapper.Map<IEnumerable<Guid>>(request);
        
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => decryptedIds.Contains(a.Id), ct);
            var toActivate = admins.Where(a => !a.IsActive).ToList();
            foreach (var admin in toActivate)
            {
                admin.IsActive = true;
            }
            if (toActivate.Any())
                await _repo.UpdateRangeAsync(toActivate, ct);
            affected = toActivate.Count;
        });
        return affected;
    }

    public async Task<int> DeactivateSelectedAsync(AdminIdsRequest request)
    {
        // Use AutoMapper to decrypt the IDs
        var decryptedIds = _mapper.Map<IEnumerable<Guid>>(request);
        
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => decryptedIds.Contains(a.Id), ct);
            var toDeactivate = admins.Where(a => a.IsActive).ToList();
            foreach (var admin in toDeactivate)
            {
                admin.IsActive = false;
            }
            if (toDeactivate.Any())
                await _repo.UpdateRangeAsync(toDeactivate, ct);
            affected = toDeactivate.Count;
        });
        return affected;
    }

    public async Task<int> ActivateAllAsync()
    {
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.GetAllAsync(ct);
            var toActivate = admins.Where(a => !a.IsActive).ToList();
            foreach (var admin in toActivate)
            {
                admin.IsActive = true;
            }
            if (toActivate.Any())
                await _repo.UpdateRangeAsync(toActivate, ct);
            affected = toActivate.Count;
        });
        return affected;
    }

    public async Task<int> DeactivateAllAsync()
    {
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.GetAllAsync(ct);
            var toDeactivate = admins.Where(a => a.IsActive).ToList();
            foreach (var admin in toDeactivate)
            {
                admin.IsActive = false;
            }
            if (toDeactivate.Any())
                await _repo.UpdateRangeAsync(toDeactivate, ct);
            affected = toDeactivate.Count;
        });
        return affected;
    }

    public async Task<int> DeleteSelectedAsync(AdminIdsRequest request)
    {
        // Use AutoMapper to decrypt the IDs
        var decryptedIds = _mapper.Map<IEnumerable<Guid>>(request);
        
        int deleted = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => decryptedIds.Contains(a.Id), ct);
            var deleteIds = admins.Select(a => a.Id).ToList();
            if (deleteIds.Any())
                await _repo.DeleteRangeAsync(deleteIds, ct);
            deleted = deleteIds.Count;
        });
        return deleted;
    }

    public async Task<int> DeleteAllExceptAsync(Guid exceptId)
    {
        int deleted = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => a.Id != exceptId, ct);
            var deleteIds = admins.Select(a => a.Id).ToList();
            if (deleteIds.Any())
                await _repo.DeleteRangeAsync(deleteIds, ct);
            deleted = deleteIds.Count;
        });
        return deleted;
    }

    // ===== Profile Management Methods =====

    public async Task<ProfileDto?> GetProfileAsync(Guid adminId)
    {
        var admin = await _repo.GetByIdAsync(adminId, ["AdminType"]);
        if (admin == null) return null;
        
        return _mapper.Map<ProfileDto>(admin);
    }

    public async Task<ProfileStatisticsDto> GetProfileStatisticsAsync(Guid adminId)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        // Calculate statistics
        var daysSinceCreation = (DateTime.UtcNow - admin.CreatedTimestamp).Days;
        var daysSincePasswordChange = admin.LastPasswordChangeAt.HasValue
            ? (DateTime.UtcNow - admin.LastPasswordChangeAt.Value).Days
            : daysSinceCreation;

        // Count entities in the system
        // Note: Since entities don't have CreatedBy field, we count all active entities
        // This provides useful statistics about system usage visible to the admin
        var allCompanies = await _companyRepo.GetAllAsync(null);
        var companiesCount = allCompanies.Count();
        
        var allSubscriptions = await _subscriptionRepo.GetAllAsync(null);
        var subscriptionsCount = allSubscriptions.Count();
        
        // Count admins created after this admin (assumes this admin may have created them)
        var allAdmins = await _repo.GetAllAsync(null);
        var adminsCreatedAfter = allAdmins.Count(a => a.CreatedTimestamp > admin.CreatedTimestamp);

        return new ProfileStatisticsDto
        {
            TotalLogins = admin.LoginCount,
            LastLoginAt = admin.LastLoginAt,
            DaysSinceCreation = daysSinceCreation,
            DaysSinceLastPasswordChange = daysSincePasswordChange,
            CompaniesManaged = companiesCount, // Total active companies in system
            SubscriptionsManaged = subscriptionsCount, // Total active subscriptions in system
            AdminsCreated = adminsCreatedAfter // Admins created after this admin
        };
    }

    public async Task<ProfileDto?> UpdateProfileAsync(Guid adminId, UpdateProfileRequest request)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null) return null;

        // Map only provided fields (AutoMapper configured with Condition)
        _mapper.Map(request, admin);

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        return await GetProfileAsync(adminId);
    }

    public async Task<ProfileDto?> UpdatePreferencesAsync(Guid adminId, UpdatePreferencesRequest request)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null) return null;

        // Update preferences
        if (request.PreferredLanguage != null) admin.PreferredLanguage = request.PreferredLanguage;
        if (request.Timezone != null) admin.Timezone = request.Timezone;
        if (request.ThemePreference != null) admin.ThemePreference = request.ThemePreference;
        if (request.DateFormat != null) admin.DateFormat = request.DateFormat;
        if (request.TimeFormat != null) admin.TimeFormat = request.TimeFormat;

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        return await GetProfileAsync(adminId);
    }

    public async Task<ProfileDto?> UpdateNotificationPreferencesAsync(Guid adminId, UpdateNotificationPreferencesRequest request)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null) return null;

        // Update all notification preferences
        admin.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        admin.PushNotificationsEnabled = request.PushNotificationsEnabled;
        admin.CompanyExpiryNotifications = request.CompanyExpiryNotifications;
        admin.SubscriptionExpiryNotifications = request.SubscriptionExpiryNotifications;
        admin.SystemAlertsNotifications = request.SystemAlertsNotifications;

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        return await GetProfileAsync(adminId);
    }

    public async Task<ProfileDto?> UploadProfilePictureAsync(Guid adminId, UploadProfilePictureRequest request)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null) return null;

        try
        {
            // Decode base64 image
            var base64Data = request.Base64Image;
            if (base64Data.Contains(","))
            {
                base64Data = base64Data.Split(',')[1]; // Remove data:image/jpeg;base64, prefix
            }
            
            var imageBytes = Convert.FromBase64String(base64Data);
            
            // Validate image size (max 5MB)
            if (imageBytes.Length > 5 * 1024 * 1024)
            {
                throw new BadRequestException(_localizer["ProfilePicture.TooLarge"]);
            }

            // Load and process image
            using var image = Image.Load(imageBytes);
            
            // Resize to 400x400 (square)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(400, 400),
                Mode = ResizeMode.Crop
            }));

            // Create uploads directory if not exists
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Delete old profile picture if exists
            if (!string.IsNullOrEmpty(admin.ProfilePictureUrl))
            {
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", admin.ProfilePictureUrl.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            // Save new image
            var fileName = $"profile_{adminId}.jpg";
            var filePath = Path.Combine(uploadsPath, fileName);
            await image.SaveAsync(filePath, new JpegEncoder { Quality = 90 });

            // Update admin profile picture URL
            admin.ProfilePictureUrl = $"/uploads/profiles/{fileName}";

            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            return await GetProfileAsync(adminId);
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            throw new BadRequestException(_localizer["ProfilePicture.InvalidFormat"]);
        }
    }

    public async Task<ProfileDto?> DeleteProfilePictureAsync(Guid adminId)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null) return null;

        // Delete actual file from storage
        if (!string.IsNullOrEmpty(admin.ProfilePictureUrl))
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", admin.ProfilePictureUrl.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // File deletion failed, continue anyway
            }
        }

        admin.ProfilePictureUrl = null;

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        return await GetProfileAsync(adminId);
    }

    // ===== Two-Factor Authentication Methods =====

    public async Task<TwoFactorSetupDto> Generate2FASecretAsync(Guid adminId)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        // Generate cryptographically secure secret
        var key = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(key);
        
        admin.TwoFactorSecret = base32Secret;

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        // Generate QR code URL for authenticator apps
        var qrCodeUrl = $"otpauth://totp/SYNFLOX:{admin.Username}?secret={base32Secret}&issuer=SYNFLOX";
        
        // Generate QR code image
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        var qrCodeBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";

        return new TwoFactorSetupDto
        {
            Secret = base32Secret,
            QRCodeBase64 = qrCodeBase64,
            ManualEntryKey = FormatSecretKey(base32Secret),
            AccountName = admin.Username,
            Issuer = "SYNFLOX"
        };
    }

    public async Task<bool> Verify2FAAsync(Guid adminId, string verificationCode)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null || string.IsNullOrEmpty(admin.TwoFactorSecret))
            return false;

        // Verify TOTP code
        var secretBytes = Base32Encoding.ToBytes(admin.TwoFactorSecret);
        var totp = new Totp(secretBytes);
        
        // Allow 1 step before and after current time (30 seconds window each side)
        var isValid = totp.VerifyTotp(verificationCode, out long timeStepMatched, new VerificationWindow(2, 2));

        if (isValid)
        {
            admin.IsTwoFactorEnabled = true;
            
            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
            
            return true;
        }

        return false;
    }

    public async Task Disable2FAAsync(Guid adminId)
    {
        var admin = await _repo.GetByIdAsync(adminId, null);
        if (admin == null)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        admin.IsTwoFactorEnabled = false;
        admin.TwoFactorSecret = null;

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
    }

    // ===== Private Helper Methods =====


    private string FormatSecretKey(string secret)
    {
        // Format as groups of 4 characters for easier manual entry
        // Example: ABCD EFGH IJKL MNOP
        return string.Join(" ", Enumerable.Range(0, secret.Length / 4)
            .Select(i => secret.Substring(i * 4, 4)));
    }
}

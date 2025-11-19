using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using Application.Services;
using AutoMapper;
using Domain.Entities.Authentication;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using OtpNet;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing admin profile operations (current user only)
/// Handles profile updates, preferences, 2FA, password changes, and security settings
/// Separated from AdminService for Single Responsibility Principle
/// </summary>
public class AdminProfileService : IAdminProfileService
{
    private readonly IAdminRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyRepository _companyRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IEmailService _emailService;
    private readonly IBackupCodeService _backupCodeService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminProfileService(
        IAdminRepository repo,
        IPasswordHasher hasher,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICompanyRepository companyRepo,
        ISubscriptionRepository subscriptionRepo,
        IEmailService emailService,
        IBackupCodeService backupCodeService,
        IHttpContextAccessor httpContextAccessor)
    {
        _repo = repo;
        _hasher = hasher;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _companyRepo = companyRepo;
        _subscriptionRepo = subscriptionRepo;
        _emailService = emailService;
        _backupCodeService = backupCodeService;
        _httpContextAccessor = httpContextAccessor;
    }

    // ===== Profile Information =====

    public async Task<ProfileDto?> GetMyProfileAsync(Guid currentUserId)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, ["AdminType"]);
        if (admin == null) return null;

        return _mapper.Map<ProfileDto>(admin);
    }

    public async Task<ProfileStatisticsDto> GetMyStatisticsAsync(Guid currentUserId)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        // Calculate statistics
        var daysSinceCreation = (DateTime.UtcNow - admin.CreatedTimestamp).Days;
        var daysSincePasswordChange = admin.LastPasswordChangeAt.HasValue
            ? (DateTime.UtcNow - admin.LastPasswordChangeAt.Value).Days
            : daysSinceCreation;

        // Count entities created by this admin
        var allCompanies = await _companyRepo.GetAllAsync(null);
        var companiesCreatedByAdmin = allCompanies.Count(c => c.CreatedBy == currentUserId);

        var allSubscriptions = await _subscriptionRepo.GetAllAsync(null);
        var subscriptionsCreatedByAdmin = allSubscriptions.Count(s => s.CreatedBy == currentUserId);

        // Count admins created by this admin
        var allAdmins = await _repo.GetAllAsync(null);
        var adminsCreatedByAdmin = allAdmins.Count(a => a.CreatedBy == currentUserId);

        return new ProfileStatisticsDto
        {
            TotalLogins = admin.LoginCount,
            LastLoginAt = admin.LastLoginAt,
            DaysSinceCreation = daysSinceCreation,
            DaysSinceLastPasswordChange = daysSincePasswordChange,
            CompaniesManaged = companiesCreatedByAdmin,
            SubscriptionsManaged = subscriptionsCreatedByAdmin,
            AdminsCreated = adminsCreatedByAdmin
        };
    }

    // ===== Profile Updates =====

    public async Task<ProfileDto?> UpdateMyProfileAsync(Guid currentUserId, UpdateProfileRequest request)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null) return null;

        // Check email uniqueness if email is being changed
        string? oldEmail = null;
        bool emailChanged = false;

        if (request.Email != null && request.Email != admin.Email)
        {
            var existingAdmin = (await _repo.FindAsync(a => a.Email == request.Email && a.Id != currentUserId)).FirstOrDefault();
            if (existingAdmin != null)
                throw new BadRequestException(_localizer["Email.AlreadyInUse"]);

            // Store old email for notification
            oldEmail = admin.Email;
            emailChanged = true;
        }

        // Check backup email doesn't match primary email or another admin's email
        if (request.BackupEmail != null && !string.IsNullOrWhiteSpace(request.BackupEmail))
        {
            if (request.BackupEmail == (request.Email ?? admin.Email))
                throw new BadRequestException(_localizer["Email.BackupCannotMatchPrimary"]);
        }

        // Validate DateOfBirth if provided
        if (request.DateOfBirth.HasValue)
        {
            // Cannot be in the future
            if (request.DateOfBirth.Value > DateTime.UtcNow)
                throw new BadRequestException(_localizer["DateOfBirth.FutureDate"]);

            // Must be at least 18 years old
            var age = DateTime.UtcNow.Year - request.DateOfBirth.Value.Year;
            if (request.DateOfBirth.Value > DateTime.UtcNow.AddYears(-age)) age--;

            if (age < 18)
                throw new BadRequestException(_localizer["DateOfBirth.MustBe18"]);

            // Reasonable maximum age (e.g., 120 years)
            if (age > 120)
                throw new BadRequestException(_localizer["DateOfBirth.Invalid"]);
        }

        // Map only provided fields (AutoMapper configured with Condition)
        _mapper.Map(request, admin);

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        // Send email notification if email was changed
        if (emailChanged && oldEmail != null && !string.IsNullOrEmpty(admin.Email))
        {
            var adminName = $"{admin.FirstName} {admin.LastName}".Trim();
            if (string.IsNullOrEmpty(adminName)) adminName = admin.Username;

            // Send asynchronously without waiting (fire and forget for better performance)
            _ = _emailService.SendEmailChangedNotificationAsync(oldEmail, admin.Email, adminName, admin.PreferredLanguage);
        }

        return await GetMyProfileAsync(currentUserId);
    }

    public async Task<ProfileDto?> UpdateMyPreferencesAsync(Guid currentUserId, UpdatePreferencesRequest request)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null) return null;

        // Validate and update preferences
        if (request.PreferredLanguage != null)
        {
            var validLanguages = new[] { "en", "ar" };
            if (!validLanguages.Contains(request.PreferredLanguage.ToLower()))
                throw new BadRequestException(_localizer["Language.Invalid"]);
        }

        if (request.ThemePreference != null)
        {
            var validThemes = new[] { "light", "dark", "auto" };
            if (!validThemes.Contains(request.ThemePreference.ToLower()))
                throw new BadRequestException(_localizer["Theme.Invalid"]);
        }

        if (request.Timezone != null)
        {
            // Validate timezone exists
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(request.Timezone);
            }
            catch
            {
                throw new BadRequestException(_localizer["TimeZone.Invalid"]);
            }
        }

        // Map preferences
        _mapper.Map(request, admin);

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        return await GetMyProfileAsync(currentUserId);
    }

    public async Task<ProfileDto?> UpdateMyNotificationPreferencesAsync(Guid currentUserId, UpdateNotificationPreferencesRequest request)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null) return null;

        _mapper.Map(request, admin);

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        return await GetMyProfileAsync(currentUserId);
    }

    // ===== Profile Picture =====

    public async Task<ProfileDto?> UploadMyProfilePictureAsync(Guid currentUserId, UploadProfilePictureRequest request)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        // Decode base64 image
        byte[] imageBytes;
        try
        {
            var base64Data = request.Base64Image;
            if (base64Data.Contains(","))
            {
                base64Data = base64Data.Split(',')[1];
            }
            imageBytes = Convert.FromBase64String(base64Data);
        }
        catch
        {
            throw new BadRequestException(_localizer["ProfilePicture.InvalidFormat"]);
        }

        // Validate image size (max 5MB)
        const int maxSizeBytes = 5 * 1024 * 1024;
        if (imageBytes.Length > maxSizeBytes)
            throw new BadRequestException(_localizer["ProfilePicture.TooLarge"]);

        // Process image using ImageSharp
        using var image = Image.Load(imageBytes);

        // Resize to 300x300
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(300, 300),
            Mode = ResizeMode.Crop
        }));

        // Save image to uploads folder
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "profiles");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"profile_{currentUserId}.jpg";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save new image first (before deleting old to prevent data loss)
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.SaveAsync(stream, new JpegEncoder { Quality = 90 });
        }

        // Update profile picture URL in database
        var oldPictureUrl = admin.ProfilePictureUrl;
        admin.ProfilePictureUrl = $"/uploads/profiles/{fileName}";

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        return await GetMyProfileAsync(currentUserId);
    }

    public async Task<ProfileDto?> DeleteMyProfilePictureAsync(Guid currentUserId)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        if (!string.IsNullOrEmpty(admin.ProfilePictureUrl))
        {
            // Delete file from disk
            var fileName = Path.GetFileName(admin.ProfilePictureUrl);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "profiles", fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            admin.ProfilePictureUrl = null;
            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
        }

        return await GetMyProfileAsync(currentUserId);
    }

    // ===== Password Management =====

    public async Task ChangeMyPasswordAsync(Guid currentUserId, ChangePasswordRequest request)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        bool valid = _hasher.VerifyPassword(request.CurrentPassword, admin.Password);
        if (!valid)
            throw new BadRequestException(_localizer["InvalidCurrentPassword"]);

        admin.Password = _hasher.HashPassword(request.NewPassword);
        admin.LastPasswordChangeAt = DateTime.UtcNow;

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        // Send password change notification email
        if (!string.IsNullOrEmpty(admin.Email))
        {
            var adminName = $"{admin.FirstName} {admin.LastName}".Trim();
            if (string.IsNullOrEmpty(adminName)) adminName = admin.Username;

            // Send asynchronously without waiting (fire and forget)
            _ = _emailService.SendPasswordChangedNotificationAsync(admin.Email, adminName, admin.PreferredLanguage);
        }
    }

    // ===== Two-Factor Authentication =====

    public async Task<TwoFactorSetupDto> Enable2FAAsync(Guid currentUserId)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        // Generate cryptographically secure secret
        var key = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(key);

        admin.TwoFactorSecret = base32Secret;
        admin.IsTwoFactorEnabled = false;
        admin.LastTwoFactorCodeUsedAt = null; // Clear previous code usage timestamp

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

    public async Task<bool> Verify2FAAsync(Guid currentUserId, string verificationCode)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null || string.IsNullOrEmpty(admin.TwoFactorSecret))
            return false;

        // Security: Ensure admin is active and not deleted
        if (!admin.IsActive || admin.IsDeleted)
            return false;

        // Prevent code reuse within 90-second window
        if (admin.LastTwoFactorCodeUsedAt.HasValue)
        {
            var timeSinceLastUse = DateTime.UtcNow - admin.LastTwoFactorCodeUsedAt.Value;
            if (timeSinceLastUse.TotalSeconds < 90)
            {
                return false; // Code was recently used, prevent replay attack
            }
        }

        var secretBytes = Base32Encoding.ToBytes(admin.TwoFactorSecret);
        var totp = new Totp(secretBytes);
        var isValid = totp.VerifyTotp(verificationCode, out long timeStepMatched, new VerificationWindow(2, 2));

        if (isValid)
        {
            admin.IsTwoFactorEnabled = true;
            admin.LastTwoFactorCodeUsedAt = DateTime.UtcNow; // Track code usage timestamp
            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task Disable2FAAsync(Guid currentUserId, string currentPassword)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null || admin.IsDeleted)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        // SECURITY: Check if admin account is active
        if (!admin.IsActive)
        {
            throw new BadRequestException(_localizer["Account.Deactivated"] ?? "Account is deactivated");
        }

        // SECURITY: Validate password length to prevent DoS
        if (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 1000)
        {
            throw new BadRequestException(_localizer["InvalidCurrentPassword"] ?? "Invalid password");
        }

        // SECURITY: Verify current password before disabling 2FA
        if (!_hasher.VerifyPassword(currentPassword, admin.Password))
        {
            throw new BadRequestException(_localizer["InvalidCurrentPassword"] ?? "Invalid password");
        }

        // Disable 2FA
        admin.IsTwoFactorEnabled = false;
        admin.TwoFactorSecret = null;
        admin.LastTwoFactorCodeUsedAt = null;

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();

        // SECURITY: Delete all backup codes when disabling 2FA
        await _backupCodeService.DeleteAllBackupCodesAsync(currentUserId);

        // Send email notification about 2FA being disabled
        if (!string.IsNullOrEmpty(admin.Email))
        {
            var adminName = $"{admin.FirstName} {admin.LastName}".Trim();
            if (string.IsNullOrEmpty(adminName)) adminName = admin.Username;
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            
            // Send asynchronously without waiting (fire and forget)
            _ = _emailService.Send2FADisabledEmailAsync(admin.Email, adminName, ipAddress, admin.PreferredLanguage);
        }
    }

    public async Task<TwoFactorSetupDto> Reset2FAAsync(Guid currentUserId, string currentPassword)
    {
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin == null || admin.IsDeleted)
            throw new NotFoundException(_localizer["Admin.NotFound"]);

        // SECURITY: Check if admin account is active
        if (!admin.IsActive)
        {
            throw new BadRequestException(_localizer["Account.Deactivated"] ?? "Account is deactivated");
        }

        // SECURITY: Validate password length to prevent DoS
        if (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 1000)
        {
            throw new BadRequestException(_localizer["InvalidCurrentPassword"] ?? "Invalid password");
        }

        // SECURITY: Verify current password before resetting 2FA
        if (!_hasher.VerifyPassword(currentPassword, admin.Password))
        {
            throw new BadRequestException(_localizer["InvalidCurrentPassword"] ?? "Invalid password");
        }

        // SECURITY: Delete all old backup codes before generating new 2FA secret
        await _backupCodeService.DeleteAllBackupCodesAsync(currentUserId);

        // Generate NEW cryptographically secure secret
        var key = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(key);

        admin.TwoFactorSecret = base32Secret;
        // Keep IsTwoFactorEnabled = true (user is resetting, not disabling)
        // They must verify the new secret to complete the reset
        admin.LastTwoFactorCodeUsedAt = null; // Clear previous code usage timestamp

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

        // Send email notification about 2FA being reset
        if (!string.IsNullOrEmpty(admin.Email))
        {
            var adminName = $"{admin.FirstName} {admin.LastName}".Trim();
            if (string.IsNullOrEmpty(adminName)) adminName = admin.Username;
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            
            // Send asynchronously without waiting (fire and forget)
            _ = _emailService.Send2FAResetEmailAsync(admin.Email, adminName, ipAddress, admin.PreferredLanguage);
        }

        return new TwoFactorSetupDto
        {
            Secret = base32Secret,
            QRCodeBase64 = qrCodeBase64,
            ManualEntryKey = base32Secret,
            AccountName = admin.Username,
            Issuer = "SYNFLOX"
        };
    }

    // ===== Account Management =====

    public async Task<bool> DeleteMyAccountAsync(Guid currentUserId)
    {
        // Protect root superadmin from self-deletion
        var admin = await _repo.GetByIdAsync(currentUserId, null);
        if (admin != null && admin.Username.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(_localizer["SuperAdmin.CannotDeleteSelf"]);
        }

        await _repo.DeleteAsync(currentUserId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    // ===== Private Helper Methods =====

    private string FormatSecretKey(string secret)
    {
        // Format: XXXX XXXX XXXX XXXX (groups of 4)
        var formatted = "";
        for (int i = 0; i < secret.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
                formatted += " ";
            formatted += secret[i];
        }
        return formatted;
    }
}

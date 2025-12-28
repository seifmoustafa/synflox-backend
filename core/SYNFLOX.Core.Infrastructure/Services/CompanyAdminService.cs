using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.CompanyAdmin;
using Application.Services;
using AutoMapper;
using Domain.Entities.Authentication;
using Domain.Entities.Licensing;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Infrastructure.Authentication;
using Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Service implementation for managing company administrator accounts.
/// </summary>
public class CompanyAdminService : ICompanyAdminService
{
    private readonly ICompanyAdminRepository _adminRepo;
    private readonly ICompanyAdminSessionRepository _sessionRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<CompanyAdminService> _logger;

    public CompanyAdminService(
        ICompanyAdminRepository adminRepo,
        ICompanyAdminSessionRepository sessionRepo,
        ICompanyRepository companyRepo,
        ISubscriptionRepository subscriptionRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILocalizationService localizer,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtOptions> jwtOptions,
        ILogger<CompanyAdminService> logger
    )
    {
        _adminRepo = adminRepo;
        _sessionRepo = sessionRepo;
        _companyRepo = companyRepo;
        _subscriptionRepo = subscriptionRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizer = localizer;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<CompanyAdminDto> CreateAsync(
        CreateCompanyAdminRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Decrypt CompanyId using AutoMapper
        var companyId = _mapper.Map<Guid>(request);

        // Validate company exists
        var company = await _companyRepo.GetByIdAsync(companyId, null, cancellationToken);
        if (company == null)
            throw new NotFoundException(_localizer["Company.NotFound"]);

        // Check if company already has an admin
        var existingAdmin = await _adminRepo.GetByCompanyIdAsync(companyId, cancellationToken);
        if (existingAdmin != null)
            throw new BadRequestException(_localizer["CompanyAdmin.AlreadyExists"]);

        // Check if username is available
        if (await _adminRepo.UsernameExistsAsync(request.Username, null, cancellationToken))
            throw new BadRequestException(_localizer["CompanyAdmin.UsernameExists"]);

        // Generate salt and hash password
        var salt = GenerateSalt();
        var passwordHash = HashPassword(request.Password, salt);

        var admin = new CompanyAdmin
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Username = request.Username,
            PasswordHash = passwordHash,
            Salt = salt,
            DisplayName = request.DisplayName,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = true,
            MustChangePassword = true,
            CanManageDevices = request.CanManageDevices,
            CanViewSubscriptions = request.CanViewSubscriptions,
            CanApproveReplacements = request.CanApproveReplacements,
            CanGenerateLicenses = request.CanGenerateLicenses,
            CanViewUsageReports = request.CanViewUsageReports,
            CreatedTimestamp = DateTime.UtcNow,
        };

        await _adminRepo.AddAsync(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created company admin {Username} for company {CompanyId}",
            request.Username,
            request.CompanyId
        );

        var activeSub = await _subscriptionRepo.GetActiveByCompanyIdAsync(
            companyId,
            cancellationToken
        );
        var isOffline = activeSub?.IsOffline ?? false;

        return MapToDto(admin, company.Name, isOffline);
    }

    public async Task<CompanyAdminDetailsDto?> GetByIdAsync(
        GetCompanyAdminByIdRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var adminId = _mapper.Map<Guid>(request);
        var admin = await _adminRepo.GetByIdAsync(adminId, new[] { "Company" }, cancellationToken);
        if (admin == null)
            return null;

        var activeSub = await _subscriptionRepo.GetActiveByCompanyIdAsync(
            admin.CompanyId,
            cancellationToken
        );
        var isOffline = activeSub?.IsOffline ?? false;

        return MapToDetailsDto(admin, isOffline);
    }

    /// <summary>
    /// Get admin by raw ID (for JWT-based lookups where ID is already decrypted from claims).
    /// </summary>
    public async Task<CompanyAdminDetailsDto?> GetByIdAsync(
        Guid adminId,
        CancellationToken cancellationToken = default
    )
    {
        var admin = await _adminRepo.GetByIdAsync(adminId, new[] { "Company" }, cancellationToken);
        if (admin == null)
            return null;

        var activeSub = await _subscriptionRepo.GetActiveByCompanyIdAsync(
            admin.CompanyId,
            cancellationToken
        );
        var isOffline = activeSub?.IsOffline ?? false;

        return MapToDetailsDto(admin, isOffline);
    }

    public async Task<CompanyAdminDetailsDto?> GetByCompanyIdAsync(
        GetCompanyAdminByCompanyIdRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var companyId = _mapper.Map<Guid>(request);
        var admin = await _adminRepo.GetByCompanyIdAsync(companyId, cancellationToken);
        if (admin == null)
            return null;

        var activeSub = await _subscriptionRepo.GetActiveByCompanyIdAsync(
            companyId,
            cancellationToken
        );
        var isOffline = activeSub?.IsOffline ?? false;

        return MapToDetailsDto(admin, isOffline);
    }

    public async Task<(List<CompanyAdminDto> Items, int TotalCount)> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default
    )
    {
        var allAdmins = await _adminRepo.GetAllAsync(new[] { "Company" }, cancellationToken);
        var query = allAdmins.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(a =>
                a.Username.ToLower().Contains(search)
                || (a.DisplayName != null && a.DisplayName.ToLower().Contains(search))
                || (a.Email != null && a.Email.ToLower().Contains(search))
                || a.Company.Name.ToLower().Contains(search)
            );
        }

        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        var totalCount = query.Count();
        var items = query
            .OrderBy(a => a.Company.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a, a.Company.Name))
            .ToList();

        return (items, totalCount);
    }

    public async Task<CompanyAdminDto> UpdateAsync(
        UpdateCompanyAdminByIdRequest idRequest,
        UpdateCompanyAdminRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Decrypt ID using AutoMapper
        var adminId = _mapper.Map<Guid>(idRequest);
        var admin = await _adminRepo.GetByIdAsync(adminId, new[] { "Company" }, cancellationToken);
        if (admin == null)
            throw new NotFoundException(_localizer["CompanyAdmin.NotFound"]);

        // Update fields
        admin.DisplayName = request.DisplayName;
        admin.Email = request.Email;
        admin.Phone = request.Phone;
        admin.IsActive = request.IsActive;

        // Permissions
        admin.CanManageDevices = request.CanManageDevices;
        admin.CanViewSubscriptions = request.CanViewSubscriptions;
        admin.CanApproveReplacements = request.CanApproveReplacements;
        admin.CanGenerateLicenses = request.CanGenerateLicenses;
        admin.CanViewUsageReports = request.CanViewUsageReports;
        admin.CanModifySessionSettings = request.CanModifySessionSettings;

        // Session Configuration
        admin.SessionPolicy = request.SessionPolicy;
        admin.SessionTimeoutMinutes = request.SessionTimeoutMinutes;
        admin.AutoLogoutOnInactivity = request.AutoLogoutOnInactivity;
        admin.InactivityTimeoutMinutes = request.InactivityTimeoutMinutes;
        admin.MaxFailedAttempts = request.MaxFailedAttempts;
        admin.LockoutDurationMinutes = request.LockoutDurationMinutes;
        admin.PasswordExpiryDays = request.PasswordExpiryDays;

        admin.UpdatedTimestamp = DateTime.UtcNow;

        await _adminRepo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated company admin {AdminId}", adminId);

        var activeSub = await _subscriptionRepo.GetActiveByCompanyIdAsync(
            admin.CompanyId,
            cancellationToken
        );
        var isOffline = activeSub?.IsOffline ?? false;

        return MapToDto(admin, admin.Company.Name, isOffline);
    }

    public async Task<bool> DeleteAsync(
        DeleteCompanyAdminRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Decrypt ID using AutoMapper
        var adminId = _mapper.Map<Guid>(request);
        var admin = await _adminRepo.GetByIdAsync(adminId, null, cancellationToken);
        if (admin == null)
            throw new NotFoundException(_localizer["CompanyAdmin.NotFound"]);

        // End all sessions
        await _sessionRepo.EndAllSessionsAsync(
            adminId,
            SessionEndReason.AccountDeactivated,
            "Admin account deleted",
            cancellationToken
        );

        // Soft delete
        admin.IsDeleted = true;
        admin.DeletedTimestamp = DateTime.UtcNow;
        admin.IsActive = false;

        await _adminRepo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted company admin {AdminId}", adminId);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(
        ResetPasswordByIdRequest idRequest,
        ResetAdminPasswordRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Decrypt ID using AutoMapper
        var adminId = _mapper.Map<Guid>(idRequest);
        var admin = await _adminRepo.GetByIdAsync(adminId, null, cancellationToken);
        if (admin == null)
            throw new NotFoundException(_localizer["CompanyAdmin.NotFound"]);

        var salt = GenerateSalt();
        var passwordHash = HashPassword(request.NewPassword, salt);

        await _adminRepo.UpdatePasswordAsync(adminId, passwordHash, salt, cancellationToken);

        if (request.MustChangeOnFirstLogin)
        {
            admin.MustChangePassword = true;
        }

        // End all sessions for security
        await _sessionRepo.EndAllSessionsAsync(
            adminId,
            SessionEndReason.PasswordChanged,
            "Password reset by admin",
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reset password for company admin {AdminId}", adminId);
        return true;
    }

    public async Task<bool> UnlockAccountAsync(
        UnlockAccountByIdRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Decrypt ID using AutoMapper
        var adminId = _mapper.Map<Guid>(request);
        var admin = await _adminRepo.GetByIdAsync(adminId, null, cancellationToken);
        if (admin == null)
            throw new NotFoundException(_localizer["CompanyAdmin.NotFound"]);

        await _adminRepo.ResetFailedLoginAsync(adminId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Unlocked company admin account {AdminId}", adminId);
        return true;
    }

    public async Task<int> TerminateAllSessionsAsync(
        TerminateSessionsByIdRequest idRequest,
        string? reason = null,
        CancellationToken cancellationToken = default
    )
    {
        // Decrypt ID using AutoMapper
        var adminId = _mapper.Map<Guid>(idRequest);
        var activeSessions = await _sessionRepo.GetActiveSessionsByAdminAsync(
            adminId,
            cancellationToken
        );
        var count = activeSessions.Count;

        await _sessionRepo.EndAllSessionsAsync(
            adminId,
            SessionEndReason.AdminTerminated,
            reason,
            cancellationToken
        );
        await _adminRepo.ClearCurrentSessionAsync(adminId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Terminated {Count} sessions for company admin {AdminId}",
            count,
            adminId
        );
        return count;
    }

    #endregion

    #region Authentication

    public async Task<AdminLoginResponse> LoginAsync(
        AdminLoginRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default
    )
    {
        var response = new AdminLoginResponse { Success = false };

        var admin = await _adminRepo.GetByUsernameAsync(request.Username, cancellationToken);
        if (admin == null)
        {
            response.Message = _localizer["CompanyAdmin.InvalidCredentials"];
            return response;
        }

        // Check if account is active
        if (!admin.IsActive)
        {
            response.Message = _localizer["CompanyAdmin.AccountInactive"];
            return response;
        }

        // Check if account is locked
        if (admin.IsLocked)
        {
            response.Message = _localizer["CompanyAdmin.AccountLocked"];
            return response;
        }

        // Verify password
        var passwordHash = HashPassword(request.Password, admin.Salt);
        if (passwordHash != admin.PasswordHash)
        {
            await _adminRepo.IncrementFailedLoginAsync(admin.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            response.Message = _localizer["CompanyAdmin.InvalidCredentials"];
            return response;
        }

        // Handle session policy
        var existingSessions = await _sessionRepo.GetActiveSessionsByAdminAsync(
            admin.Id,
            cancellationToken
        );

        if (admin.SessionPolicy == AdminSessionPolicy.SingleSession && existingSessions.Any())
        {
            // Terminate existing sessions
            await _sessionRepo.EndAllSessionsAsync(
                admin.Id,
                SessionEndReason.NewSessionStarted,
                "New login",
                cancellationToken
            );
        }

        // Create new session
        var sessionId = GenerateSessionId();
        var expiresAt = DateTime.UtcNow.AddMinutes(admin.SessionTimeoutMinutes);

        var session = new CompanyAdminSession
        {
            Id = Guid.NewGuid(),
            AdminId = admin.Id,
            SessionId = sessionId,
            DeviceHash = request.DeviceHash,
            DeviceName = request.DeviceName,
            OperatingSystem = request.OperatingSystem,
            UserAgent = request.DeviceName, // Using DeviceName as UserAgent
            IpAddress = ipAddress ?? "Unknown",
            Location = "Unknown", // Can be enhanced with IP geolocation later
            StartedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAt,
            LastActivityAtUtc = DateTime.UtcNow,
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow,
        };

        await _sessionRepo.AddAsync(session);

        // Update admin with current session info
        await _adminRepo.UpdateLastLoginAsync(admin.Id, ipAddress, cancellationToken);
        await _adminRepo.UpdateCurrentSessionAsync(
            admin.Id,
            sessionId,
            request.DeviceHash,
            request.DeviceName,
            ipAddress,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Company admin {Username} logged in from {IP}",
            admin.Username,
            ipAddress
        );

        // Generate JWT tokens
        var accessToken = _jwtTokenGenerator.GenerateCompanyAdminToken(admin);
        var refreshTokenEntity = _jwtTokenGenerator.GenerateCompanyAdminRefreshToken(admin);

        // Store refresh token
        await _refreshTokenRepo.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var activeSub = await _subscriptionRepo.GetActiveByCompanyIdAsync(
            admin.CompanyId,
            cancellationToken
        );
        var isOffline = activeSub?.IsOffline ?? false;

        response.Success = true;
        response.Message = _localizer["CompanyAdmin.LoginSuccess"];
        response.AccessToken = accessToken;
        response.RefreshToken = refreshTokenEntity.Token;
        response.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.Lifetime);
        response.RefreshTokenExpiresAtUtc = refreshTokenEntity.Expires;
        response.ExpiresIn = _jwtOptions.Lifetime * 60; // Convert to seconds
#pragma warning disable CS0618 // Obsolete member - keeping for backward compatibility
        response.Token = accessToken; // Legacy support
        response.SessionId = sessionId; // Legacy support
#pragma warning restore CS0618
        response.MustChangePassword = admin.MustChangePassword || admin.IsPasswordExpired;
        response.HasOtherActiveSessions =
            admin.SessionPolicy == AdminSessionPolicy.MultipleWithWarning && existingSessions.Any();
        response.OtherActiveSessionCount = existingSessions.Count;
        response.Admin = MapToDto(admin, admin.Company?.Name ?? "", isOffline);

        return response;
    }

    public async Task<bool> LogoutAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var session = await _sessionRepo.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session == null)
            return false;

        await _sessionRepo.EndSessionAsync(
            sessionId,
            SessionEndReason.Logout,
            null,
            cancellationToken
        );

        // If this was the current session, clear it from admin
        var admin = session.Admin;
        if (admin != null && admin.CurrentSessionId == sessionId)
        {
            await _adminRepo.ClearCurrentSessionAsync(admin.Id, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Company admin session {SessionId} logged out", sessionId);
        return true;
    }

    public async Task<AdminLoginResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default
    )
    {
        var response = new AdminLoginResponse { Success = false };

        // Find the refresh token
        var tokenEntity = await _refreshTokenRepo.GetByTokenAsync(refreshToken, cancellationToken);
        if (tokenEntity == null)
        {
            response.Message = _localizer["Auth.InvalidRefreshToken"];
            return response;
        }

        // Check if token is valid
        if (tokenEntity.IsExpired || tokenEntity.IsRevoked)
        {
            response.Message = _localizer["Auth.RefreshTokenExpired"];
            return response;
        }

        // Token must be for a CompanyAdmin
        if (!tokenEntity.CompanyAdminId.HasValue)
        {
            response.Message = _localizer["Auth.InvalidRefreshToken"];
            return response;
        }

        // Get the company admin
        var admin = await _adminRepo.GetByIdAsync(
            tokenEntity.CompanyAdminId.Value,
            new[] { "Company" },
            cancellationToken
        );
        if (admin == null || !admin.IsActive)
        {
            response.Message = _localizer["CompanyAdmin.AccountInactive"];
            return response;
        }

        // Revoke the old refresh token
        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.RevokedReason = "TokenRefresh";
        await _refreshTokenRepo.UpdateAsync(tokenEntity);

        // Generate new tokens
        var newAccessToken = _jwtTokenGenerator.GenerateCompanyAdminToken(admin);
        var newRefreshToken = _jwtTokenGenerator.GenerateCompanyAdminRefreshToken(admin);

        await _refreshTokenRepo.AddAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var activeSub = await _subscriptionRepo.GetActiveByCompanyIdAsync(
            admin.CompanyId,
            cancellationToken
        );
        var isOffline = activeSub?.IsOffline ?? false;

        response.Success = true;
        response.Message = _localizer["Auth.TokenRefreshed"];
        response.AccessToken = newAccessToken;
        response.RefreshToken = newRefreshToken.Token;
        response.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.Lifetime);
        response.RefreshTokenExpiresAtUtc = newRefreshToken.Expires;
        response.ExpiresIn = _jwtOptions.Lifetime * 60;
        response.Admin = MapToDto(admin, admin.Company?.Name ?? "", isOffline);

        _logger.LogInformation("Refreshed tokens for company admin {AdminId}", admin.Id);
        return response;
    }

    public async Task<bool> LogoutByAdminIdAsync(
        Guid adminId,
        CancellationToken cancellationToken = default
    )
    {
        // Revoke all refresh tokens for this admin
        var tokens = await _refreshTokenRepo.GetActiveByCompanyAdminIdAsync(
            adminId,
            cancellationToken
        );
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "Logout";
            await _refreshTokenRepo.UpdateAsync(token);
        }

        // Also end any legacy sessions
        await _sessionRepo.EndAllSessionsAsync(
            adminId,
            SessionEndReason.Logout,
            "JWT Logout",
            cancellationToken
        );
        await _adminRepo.ClearCurrentSessionAsync(adminId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Company admin {AdminId} logged out (JWT)", adminId);
        return true;
    }

    public async Task<CompanyAdminDetailsDto?> ValidateSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var session = await _sessionRepo.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session == null || !session.IsValid)
            return null;

        var admin = session.Admin;
        if (admin == null || !admin.IsActive)
            return null;

        // Check inactivity timeout
        if (admin.AutoLogoutOnInactivity && admin.IsSessionInactive)
        {
            await _sessionRepo.EndSessionAsync(
                sessionId,
                SessionEndReason.Inactivity,
                null,
                cancellationToken
            );
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        var activeSub = await _subscriptionRepo.GetActiveByCompanyIdAsync(
            admin.CompanyId,
            cancellationToken
        );
        var isOffline = activeSub?.IsOffline ?? false;

        return MapToDetailsDto(admin, isOffline);
    }

    public async Task<bool> RefreshSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var session = await _sessionRepo.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session == null || !session.IsValid)
            return false;

        await _sessionRepo.UpdateSessionActivityAsync(sessionId, cancellationToken);
        await _adminRepo.UpdateLastActivityAsync(session.AdminId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region Self-Service

    public async Task<bool> ChangePasswordAsync(
        Guid adminId,
        CompanyAdminChangePasswordRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var admin = await _adminRepo.GetByIdAsync(adminId, null, cancellationToken);
        if (admin == null)
            throw new NotFoundException(_localizer["CompanyAdmin.NotFound"]);

        // Verify current password
        var currentHash = HashPassword(request.CurrentPassword, admin.Salt);
        if (currentHash != admin.PasswordHash)
            throw new BadRequestException(_localizer["CompanyAdmin.InvalidCurrentPassword"]);

        // Update password
        var salt = GenerateSalt();
        var passwordHash = HashPassword(request.NewPassword, salt);

        await _adminRepo.UpdatePasswordAsync(adminId, passwordHash, salt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Company admin {AdminId} changed their password", adminId);
        return true;
    }

    public async Task<bool> UpdateSessionSettingsAsync(
        Guid adminId,
        UpdateSessionSettingsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var admin = await _adminRepo.GetByIdAsync(adminId, null, cancellationToken);
        if (admin == null)
            throw new NotFoundException(_localizer["CompanyAdmin.NotFound"]);

        if (!admin.CanModifySessionSettings)
            throw new BadRequestException(_localizer["CompanyAdmin.CannotModifySettings"]);

        admin.SessionPolicy = request.SessionPolicy;
        admin.SessionTimeoutMinutes = request.SessionTimeoutMinutes;
        admin.AutoLogoutOnInactivity = request.AutoLogoutOnInactivity;
        admin.InactivityTimeoutMinutes = request.InactivityTimeoutMinutes;

        admin.UpdatedTimestamp = DateTime.UtcNow;

        await _adminRepo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Company admin {AdminId} updated their session settings", adminId);
        return true;
    }

    public async Task<List<CompanyAdminSessionDto>> GetSessionHistoryAsync(
        Guid adminId,
        int limit = 50,
        CancellationToken cancellationToken = default
    )
    {
        var sessions = await _sessionRepo.GetSessionHistoryAsync(adminId, limit, cancellationToken);
        return sessions.Select(MapToSessionDto).ToList();
    }

    public async Task<List<CompanyAdminSessionDto>> GetActiveSessionsAsync(
        Guid adminId,
        CancellationToken cancellationToken = default
    )
    {
        var sessions = await _sessionRepo.GetActiveSessionsByAdminAsync(adminId, cancellationToken);
        return sessions.Select(MapToSessionDto).ToList();
    }

    public async Task<bool> TerminateSessionAsync(
        Guid adminId,
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var session = await _sessionRepo.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session == null || session.AdminId != adminId)
            return false;

        await _sessionRepo.EndSessionAsync(
            sessionId,
            SessionEndReason.AdminTerminated,
            "Terminated by user",
            cancellationToken
        );
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region Validation

    public async Task<bool> IsUsernameAvailableAsync(
        string username,
        Guid? excludeAdminId = null,
        CancellationToken cancellationToken = default
    )
    {
        return !await _adminRepo.UsernameExistsAsync(username, excludeAdminId, cancellationToken);
    }

    public async Task<bool> CompanyHasAdminAsync(
        CompanyHasAdminRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Debug: Log encrypted and decrypted IDs
        _logger.LogInformation(
            "CompanyHasAdminAsync - Encrypted CompanyId: {EncryptedId}",
            request.CompanyId
        );

        // Decrypt CompanyId using AutoMapper
        var companyId = _mapper.Map<Guid>(request);

        _logger.LogInformation(
            "CompanyHasAdminAsync - Decrypted CompanyId: {DecryptedId}",
            companyId
        );

        var admin = await _adminRepo.GetByCompanyIdAsync(companyId, cancellationToken);

        _logger.LogInformation("CompanyHasAdminAsync - Admin found: {Found}", admin != null);

        return admin != null;
    }

    #endregion

    #region Private Helpers

    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var combined = Encoding.UTF8.GetBytes(password + salt);
        var hash = sha256.ComputeHash(combined);
        return Convert.ToBase64String(hash);
    }

    private static string GenerateSessionId()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private CompanyAdminDto MapToDto(CompanyAdmin admin, string companyName, bool isOffline = false)
    {
        return new CompanyAdminDto
        {
            Id = admin.Id,
            CompanyId = admin.CompanyId,
            CompanyName = companyName,
            Username = admin.Username,
            DisplayName = admin.DisplayName,
            Email = admin.Email,
            Phone = admin.Phone,
            IsActive = admin.IsActive,
            MustChangePassword = admin.MustChangePassword,
            IsLocked = admin.IsLocked,
            LockedUntilUtc = admin.LockedUntilUtc,
            SessionPolicy = admin.SessionPolicy,
            SessionTimeoutMinutes = admin.SessionTimeoutMinutes,
            LastLoginAtUtc = admin.LastLoginAtUtc,
            TotalLogins = admin.TotalLogins,
            HasActiveSession = admin.HasActiveSession,
            CreatedTimestamp = admin.CreatedTimestamp,
            IsOffline = isOffline,
        };
    }

    private CompanyAdminDetailsDto MapToDetailsDto(CompanyAdmin admin, bool isOffline = false)
    {
        return new CompanyAdminDetailsDto
        {
            Id = admin.Id,
            CompanyId = admin.CompanyId,
            CompanyName = admin.Company?.Name ?? "",
            Username = admin.Username,
            DisplayName = admin.DisplayName,
            Email = admin.Email,
            Phone = admin.Phone,
            IsActive = admin.IsActive,
            MustChangePassword = admin.MustChangePassword,
            IsLocked = admin.IsLocked,
            LockedUntilUtc = admin.LockedUntilUtc,
            SessionPolicy = admin.SessionPolicy,
            SessionTimeoutMinutes = admin.SessionTimeoutMinutes,
            LastLoginAtUtc = admin.LastLoginAtUtc,
            TotalLogins = admin.TotalLogins,
            HasActiveSession = admin.HasActiveSession,
            CreatedTimestamp = admin.CreatedTimestamp,
            // Permissions
            CanManageDevices = admin.CanManageDevices,
            CanViewSubscriptions = admin.CanViewSubscriptions,
            CanApproveReplacements = admin.CanApproveReplacements,
            CanGenerateLicenses = admin.CanGenerateLicenses,
            CanViewUsageReports = admin.CanViewUsageReports,
            CanModifySessionSettings = admin.CanModifySessionSettings,
            IsOffline = isOffline,
            // Session Configuration
            AutoLogoutOnInactivity = admin.AutoLogoutOnInactivity,
            InactivityTimeoutMinutes = admin.InactivityTimeoutMinutes,
            MaxFailedAttempts = admin.MaxFailedAttempts,
            LockoutDurationMinutes = admin.LockoutDurationMinutes,
            PasswordExpiryDays = admin.PasswordExpiryDays,
            // Current Session Info
            CurrentDeviceName = admin.CurrentDeviceName,
            CurrentSessionIp = admin.CurrentSessionIp,
            SessionStartedAtUtc = admin.SessionStartedAtUtc,
            LastActivityAtUtc = admin.LastActivityAtUtc,
        };
    }

    private CompanyAdminSessionDto MapToSessionDto(CompanyAdminSession session)
    {
        return new CompanyAdminSessionDto
        {
            Id = session.Id,
            SessionId = session.SessionId,
            DeviceName = session.DeviceName,
            OperatingSystem = session.OperatingSystem,
            IpAddress = session.IpAddress,
            Location = session.Location,
            StartedAtUtc = session.StartedAtUtc,
            ExpiresAtUtc = session.ExpiresAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            LastActivityAtUtc = session.LastActivityAtUtc,
            IsActive = session.IsActive,
            EndReason = session.EndReason.ToString(),
            ActionCount = session.ActionCount,
            DurationMinutes = session.DurationMinutes,
        };
    }

    #endregion
}

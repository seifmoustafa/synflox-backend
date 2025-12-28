using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.CompanyAdmin;

namespace Application.Services;

/// <summary>
/// Service interface for managing company administrator accounts.
/// Handles CRUD operations, authentication, and session management.
/// </summary>
public interface ICompanyAdminService
{
    #region CRUD Operations (SYNFLOX Admin Only)

    /// <summary>
    /// Create a new company admin account.
    /// Only SYNFLOX admins can create these accounts.
    /// </summary>
    Task<CompanyAdminDto> CreateAsync(
        CreateCompanyAdminRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get admin by ID (uses AutoMapper to decrypt ID).
    /// </summary>
    Task<CompanyAdminDetailsDto?> GetByIdAsync(
        GetCompanyAdminByIdRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get admin by raw ID (for JWT-based lookups where ID is already decrypted from claims).
    /// </summary>
    Task<CompanyAdminDetailsDto?> GetByIdAsync(
        Guid adminId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get admin by company ID (uses AutoMapper to decrypt ID).
    /// </summary>
    Task<CompanyAdminDetailsDto?> GetByCompanyIdAsync(
        GetCompanyAdminByCompanyIdRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get all company admins (paginated).
    /// </summary>
    Task<(List<CompanyAdminDto> Items, int TotalCount)> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Update company admin account (uses AutoMapper to decrypt ID).
    /// </summary>
    Task<CompanyAdminDto> UpdateAsync(
        UpdateCompanyAdminByIdRequest idRequest,
        UpdateCompanyAdminRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Delete (soft delete) company admin account (uses AutoMapper to decrypt ID).
    /// </summary>
    Task<bool> DeleteAsync(
        DeleteCompanyAdminRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Reset admin password (uses AutoMapper to decrypt ID).
    /// </summary>
    Task<bool> ResetPasswordAsync(
        ResetPasswordByIdRequest idRequest,
        ResetAdminPasswordRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Unlock a locked admin account (uses AutoMapper to decrypt ID).
    /// </summary>
    Task<bool> UnlockAccountAsync(
        UnlockAccountByIdRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Terminate all active sessions for an admin (uses AutoMapper to decrypt ID).
    /// </summary>
    Task<int> TerminateAllSessionsAsync(
        TerminateSessionsByIdRequest idRequest,
        string? reason = null,
        CancellationToken cancellationToken = default
    );

    #endregion

    #region Authentication (Client Admin)

    /// <summary>
    /// Authenticate admin with username and password.
    /// Returns JWT access token and refresh token.
    /// </summary>
    Task<AdminLoginResponse> LoginAsync(
        AdminLoginRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Refresh JWT access token using a valid refresh token.
    /// Returns new access token and optionally a new refresh token.
    /// </summary>
    Task<AdminLoginResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Logout and revoke refresh token (JWT-based).
    /// Uses admin ID from JWT claims.
    /// </summary>
    Task<bool> LogoutByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout from current session (legacy session-based).
    /// </summary>
    [Obsolete("Use LogoutByAdminIdAsync for JWT-based logout")]
    Task<bool> LogoutAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate session and get admin context (legacy session-based).
    /// Returns null if session is invalid or expired.
    /// </summary>
    [Obsolete("Use JWT claims extraction instead")]
    Task<CompanyAdminDetailsDto?> ValidateSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Refresh session activity (legacy heartbeat).
    /// </summary>
    [Obsolete("JWT tokens handle expiry automatically")]
    Task<bool> RefreshSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    #endregion

    #region Self-Service (Client Admin)

    /// <summary>
    /// Change own password (adminId already decrypted from session).
    /// </summary>
    Task<bool> ChangePasswordAsync(
        Guid adminId,
        CompanyAdminChangePasswordRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Update own session settings (adminId already decrypted from session).
    /// </summary>
    Task<bool> UpdateSessionSettingsAsync(
        Guid adminId,
        UpdateSessionSettingsRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get own session history (adminId already decrypted from session).
    /// </summary>
    Task<List<CompanyAdminSessionDto>> GetSessionHistoryAsync(
        Guid adminId,
        int limit = 50,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get active sessions for current admin (adminId already decrypted from session).
    /// </summary>
    Task<List<CompanyAdminSessionDto>> GetActiveSessionsAsync(
        Guid adminId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Terminate a specific session (adminId already decrypted from session).
    /// </summary>
    Task<bool> TerminateSessionAsync(
        Guid adminId,
        string sessionId,
        CancellationToken cancellationToken = default
    );

    #endregion

    #region Validation

    /// <summary>
    /// Check if username is available.
    /// </summary>
    Task<bool> IsUsernameAvailableAsync(
        string username,
        Guid? excludeAdminId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Check if company has an admin account (uses AutoMapper to decrypt ID).
    /// </summary>
    Task<bool> CompanyHasAdminAsync(
        CompanyHasAdminRequest request,
        CancellationToken cancellationToken = default
    );

    #endregion
}
